using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NGY.API.Authentication.HMAC
{
    /// <summary>
    /// Message handler that performs an HMAC authentication check on each incoming API request.
    /// 
    /// With Hash-based Message Authentication Code (HMAC), a user request can be authenticated by gathering details of the API request and hashing
    /// it with a secret API key only the user and web API service know. This hash or digital signature will be sent in the header of the API
    /// request. The API service will perform the same hash computation using the same user's API key. If the hash calculated on the server and the
    /// hash in the request message headers are the same then the request is authenticated.
    /// 
    /// The hash computation cannot be reversed and can only be recreated with the user's secret API key. Since the API key itself is never
    /// transmitted over the wire (only the hash is) it is not at risk of being intercepted.
    /// 
    /// To ensure the request message is unaltered during transit, various parts of the request message will be included in the hash computation.
    /// Changing any of these message parts will result in a change to the resulting hash computation and thus it would not match the API service's
    /// own hash computation of the request it actually received.
    /// 
    /// The following request message parts will be required for HMAC authentication:
    /// 
    /// <list type="bullet">
    ///     <item>
    ///         <term>HTTP METHOD</term>
    ///         <description>The HTTP method used in the request (i.e. GET, POST, DELETE, etc.)</description>
    ///     </item>
    ///     <item>
    ///         <term>Content MD5</term>
    ///         <description>
    ///             If there is any request content, compute an MD5 hash as a byte array and convert to a base64 string. If no content is available
    ///             then simply leave this line blank. This MD5 hash will also need to be placed in the Content-MD5 header of the request if it exists.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Timestamp</term>
    ///         <description>
    ///             A timestamp of the request. If the timestamp is too old the message will be rejected. It must be in the following format which is
    ///             an [RFC1123](https://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.3.1) specification on HTTP-date headers:
    ///             
    ///             <c>Wed, 04 May 1977 16:00:00 GMT</c>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Username</term>
    ///         <description>The username of the user making the request.</description>
    ///     </item>
    ///     <item>
    ///         <term>Request Uri</term>
    ///         <description>The request URI in full including protocol and query string.</description>
    ///     </item>
    /// </list>
    /// 
    /// The following is an example of what should be constructed, each line separated with a newline character <c>\n</c>:
    /// 
    /// <code>
    /// POST
    /// 46221fb52613b6b94daf5207dbf8443f
    /// Wed, 04 May 1977 16:00:00 GMT
    /// dvader
    /// https://empire.gov/api/v1/droid/activate-restraining-bolt?id=r2d2
    /// </code>
    /// 
    /// Or with no request message content, use an empty string for the MD5 hash:
    /// 
    /// <code>
    /// GET
    /// 
    /// Wed, 04 May 1977 16:00:00 GMT
    /// dvader
    /// https://empire.gov/api/v1/droid/activate-restraining-bolt?id=r2d2
    /// </code>
    /// 
    /// </summary>
    /// <remarks>
    /// This authentication message handler has limited protection against replay attacks. As is, this is done by comparing the timestamp of the
    /// request with the time on the API server. If the time difference is too significant, the request is rejected. The timestamp is verified by
    /// being included in the digital signature. Thus any change to the timestamp would break the signature.
    /// 
    /// However, this method only prevent replay attacks outside of the time difference window (usually 5 mins). Which means within this window it is
    /// still possible to replay a request successfully. To prevent this, some sort of caching is needed which is typically platform specific, thus
    /// this process is left to the implementing project that uses this library.
    /// 
    /// One common way to prevent replay attacks during this time window is to cache the authenticated signatures of each request in memory. Since
    /// the request timestamp is part of the signature and the request is only valid for a period of time, rejecting a request with the same
    /// signature in that time window will prevent all replay attacks.
    /// 
    /// TODO : Provide example using HttpRuntime.Cache
    /// 
    /// </remarks>
    public class HmacAuthenticationHandler : DelegatingHandler
    {

        private readonly ISecretRepository _secretRepository;
        private readonly IBuildMessageRepresentation _representationBuilder;
        private readonly ICalculteSignature _signatureCalculator;

        public HmacAuthenticationHandler(ISecretRepository secretRepository, IBuildMessageRepresentation representationBuilder, ICalculteSignature signatureCalculator)
        {
            _secretRepository = secretRepository;
            _representationBuilder = representationBuilder;
            _signatureCalculator = signatureCalculator;
        }

        /// <summary>
        /// Validates the HMAC digital signature of the API request.
        /// </summary>
        /// <param name="requestMessage">The API request message to be validated for authenticity.</param>
        /// <returns><c>true</c> if authenticated successfully, <c>false</c> otherwise.</returns>
        protected async Task<bool> IsAuthenticated(HttpRequestMessage requestMessage)
        {
            // No username, no authentication.
            if (!requestMessage.Headers.Contains(HmacApiAuthConfiguration.UsernameHeader))
            {
                return false;
            }

            // No date, no authentication.
            if (!IsDateValid(requestMessage))
            {
                return false;
            }

            // No Authorization header or wrong type, no authentication.
            if (requestMessage.Headers.Authorization == null
                || requestMessage.Headers.Authorization.Scheme != HmacApiAuthConfiguration.AuthenticationScheme)
            {
                return false;
            }

            // No ContentMD5 header yet there is Content available, no authentication.
            // No match between ContentMD5 header and actual Content MD5 hash, no authentication.
            if (!await IsMd5Valid(requestMessage))
            {
                return false;
            }

            // Using the user's key we will now calculate what the HMAC signature should be based on the provided request message.
            // After building our own copy of the signature we will compare it to the signature provided in the API request.

            // Retrieve the username and validate user has a registered key.
            string username = requestMessage.Headers.GetValues(HmacApiAuthConfiguration.UsernameHeader).First();
            var secret = _secretRepository.GetSecretForUser(username);
            if (secret == null)
            {
                return false;
            }

            // Build string representation of request. The client should have built their request the same way.
            var representation = _representationBuilder.BuildRequestRepresentation(requestMessage);
            if (representation == null)
            {
                return false;
            }

            // Calculate our version of the HMAC signature and compare it to the request message signature to validate authentication.
            var signature = _signatureCalculator.Signature(secret, representation);
            var result = requestMessage.Headers.Authorization.Parameter == signature;

            return result;
        }

        /// <summary>
        /// Validates the request message content header, <c>ContentMD5</c>, matches a newly calculated MD5 hash of the content.
        /// </summary>
        /// <param name="requestMessage">The request message to have its content validated.</param>
        /// <returns><c>true</c> if the MD5 hash of the request content matches the <c>ContentMD5</c> content header; <c>false</c> otherwise.</returns>
        private async Task<bool> IsMd5Valid(HttpRequestMessage requestMessage)
        {
            var content = requestMessage.Content;
            var contentMD5Header = requestMessage.Content?.Headers?.ContentMD5;

            // Validate that if there is no content then the there is also no ContentMD5 header (i.e. message body wasn't removed).
            if (content == null || content.Headers.ContentLength == 0)
            {
                return (contentMD5Header == null) || (contentMD5Header.Length == 0);
            }

            // Validate that if there is content then there is also a ContentMD5 header (i.e. contentMD5 header wasn't removed or left out).
            else if ((content.Headers.ContentLength > 0) && ((contentMD5Header == null) || (contentMD5Header.Length == 0)))
            {
                return false;
            }

            // Validate the ContentMD5 header matches our calculated hash.
            var hash = await MD5Helper.ComputeHash(content);
            return hash.SequenceEqual(contentMD5Header);
        }

        /// <summary>
        /// Validates the request message header, <c>Date</c>, contains a date and time within the validity period allowed by the web API service.
        /// </summary>
        /// <param name="requestMessage">The request message containing date header to be validated.</param>
        /// <returns><c>true</c> if the <c>Date</c> header is within the validity period; <c>false</c> otherwise.</returns>
        private bool IsDateValid(HttpRequestMessage requestMessage)
        {
            var utcNow = DateTime.UtcNow;
            var date = requestMessage.Headers.Date.GetValueOrDefault().UtcDateTime;

            // The date cannot be + or - the validity period.
            if (date >= utcNow.AddMinutes(HmacApiAuthConfiguration.ValidityPeriodInMinutes)
                || date <= utcNow.AddMinutes(-HmacApiAuthConfiguration.ValidityPeriodInMinutes))
            {
                return false;
            }

            return true;
        }

        /// <inheritDoc/>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var isAuthenticated = await IsAuthenticated(request);

            // If authentication failed, send an Unauthorized HTTP Error response.
            if (!isAuthenticated)
            {
                var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                response.RequestMessage = request;
                response.ReasonPhrase = HmacApiAuthConfiguration.UnauthorizedMessage;
                response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue(HmacApiAuthConfiguration.AuthenticationScheme));

                return response;
            }

            // If authenticated, pass request on to inner message handlers for processing and response.
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

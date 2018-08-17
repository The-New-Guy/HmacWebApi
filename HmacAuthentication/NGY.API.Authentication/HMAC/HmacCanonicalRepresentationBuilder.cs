using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;

namespace NGY.API.Authentication.HMAC
{
    /// <summary>
    /// Builder class used to create the appropriate cannonical string representation of the API request message using various message headers,
    /// content and other information required by the API authentication signature.
    /// </summary>
    public class HmacCanonicalRepresentationBuilder : IBuildMessageRepresentation
    {

        /// <summary>
        /// Builds a cannonical string representation of the API request message using various message headers, content and other information
        /// required by the API authentication signature.
        /// 
        /// Builds message representation using the following request properties:
        /// 
        ///     HTTP METHOD
        ///     Content-MD5
        ///     Timestamp
        ///     Username
        ///     Request URI
        /// 
        /// The following is an example of what will be constructed, each line separated with a newline character <c>\n</c>:
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
        /// </summary>
        /// <returns>Cannonical string representation of the API request message.</returns>
        public string BuildRequestRepresentation(HttpRequestMessage requestMessage)
        {
            // Validate the request message has the correct headers and information required.
            if (!IsRequestValid(requestMessage))
            {
                return null;
            }

            // Get the request date which should already be stamped on the request.
            DateTimeOffset requestDate = requestMessage.Headers.Date.Value;

            // Get MD5 hash of content and base64 encode it. If there is no content just use an empty string.
            string md5 = (requestMessage.Content == null ||
                          requestMessage.Content.Headers.ContentMD5 == null) ? "" : Convert.ToBase64String(requestMessage.Content.Headers.ContentMD5);

            // Get request method, username and uri.
            string httpMethod = requestMessage.Method.Method;
            string username = requestMessage.Headers.GetValues(HmacApiAuthConfiguration.UsernameHeader).First();
            string uri = requestMessage.RequestUri.AbsoluteUri.ToLower();

            // Build final representation.
            string representation = String.Join("\n", httpMethod, md5, requestDate.ToString("r"), username, uri);

            return representation;
        }

        /// <summary>
        /// Verifies the request has all the correct headers and information required for authentication requests.
        /// </summary>
        /// <param name="requestMessage">The HttpRequestMessage to validate.</param>
        /// <returns><c>true</c> if the required request headers are present; <c>false</c> otherwise.</returns>
        private bool IsRequestValid(HttpRequestMessage requestMessage)
        {
            // Validate header has a data value.
            if (!requestMessage.Headers.Date.HasValue)
            {
                return false;
            }

            // Validate header has the custom username field.
            if (!requestMessage.Headers.Contains(HmacApiAuthConfiguration.UsernameHeader))
            {
                return false;
            }

            // Validate content header, if available, is of correct media type (i.e. application/json, etc.) and has an associated ContentMD5 header.
            if (requestMessage.Content != null && requestMessage.Content.Headers.ContentLength > 0)
            {
                if (requestMessage.Content.Headers.ContentType == null ||
                    !HmacApiAuthConfiguration.ValidContentMediaTypes.Contains(requestMessage.Content.Headers.ContentType.MediaType.ToLower()))
                {
                    return false;
                }

                if ((requestMessage.Content.Headers.ContentMD5 == null) || (requestMessage.Content.Headers.ContentMD5.Length == 0))
                {
                    return false;
                }
            }

            return true;
        }

    }
}

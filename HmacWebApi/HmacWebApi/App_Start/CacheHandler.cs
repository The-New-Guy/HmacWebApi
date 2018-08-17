using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NGY.API.Authentication;
using NGY.API.Authentication.HMAC;

namespace HmacWebApi
{
    /// <summary>
    /// A message handler that handles temporary caching of API request. This cache is then checked on all incoming requests to ensure not one request
    /// is ever replayed.
    /// 
    /// TODO : At the moment however it is just a wrapper to allow me to test the authentication handler.
    /// </summary>
    public class CacheHandler : HmacAuthenticationHandler
    {
        ISecretRepository _secretRepo;
        IBuildMessageRepresentation _representBuilder;
        ICalculteSignature _sigCalc;

        public CacheHandler(ISecretRepository secretRepository, IBuildMessageRepresentation representationBuilder, ICalculteSignature signatureCalculator)
            : base(secretRepository, representationBuilder, signatureCalculator)
        {
            _secretRepo = secretRepository;
            _representBuilder = representationBuilder;
            _sigCalc = signatureCalculator;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string reqSignature = "";
            HttpResponseMessage response = null;

            // Get digital signature if available.
            if (request.Headers?.Authorization != null && request.Headers?.Authorization.Scheme == HmacApiAuthConfiguration.AuthenticationScheme)
            {
                reqSignature = request.Headers.Authorization.Parameter;
            }
            else
            {
                // We should just return here if there is no signature.
                return request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized request : Missing or invalid signature");
            }

            // TODO : Check to see if signature is currently in cache. If so return now.

            // TODO : Cache signature in memory for the validity period (5 mins) to ensure no request gets replayed.

            try
            {
                // Call the base authentication handler.
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (Exception exception)
            {

                // Catch any authentication error messages and provide custom error message response.
                response = request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error authenticating");
                var respMsg = new StringBuilder();
                respMsg.AppendLine("Error authenticating...");

                respMsg.AppendLine("StatusCode            : " + response.StatusCode);
                respMsg.AppendLine("ReasonPhrase          : " + response.ReasonPhrase);
                respMsg.AppendLine("WwwAuthenticate       : " + response.Headers.WwwAuthenticate.FirstOrDefault().ToString());
                respMsg.AppendLine("RequestDate           : " + request.Headers.Date.GetValueOrDefault().UtcDateTime.ToString(CultureInfo.InvariantCulture));
                respMsg.AppendLine("ServerDate            : " + (DateTimeOffset.Now));

                respMsg.AppendLine();

                respMsg.AppendLine("ExceptionMessage      : " + exception.Message);
                respMsg.AppendLine("ExceptionSource       : " + exception.Source);
                respMsg.AppendLine("ExceptionInnerMessage : " + exception.InnerException?.Message);
                respMsg.AppendLine("ExceptionStackTrace   : " + exception.StackTrace);
                response.Content = new StringContent(respMsg.ToString());

            }

            // Catch any authentication failure message and provide custom error message response.
            if (response != null && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var serverDate = DateTimeOffset.Now;
                var respMsg = new StringBuilder();

                respMsg.AppendLine("Authentication failed");

                respMsg.AppendLine();

                respMsg.AppendLine("Basic Details\n");

                respMsg.AppendLine("URL              : " + request.RequestUri.AbsoluteUri.ToLower());
                respMsg.AppendLine("StatusCode       : " + response.StatusCode);
                respMsg.AppendLine("ReasonPhrase     : " + response.ReasonPhrase);
                //respMsg.AppendLine("WwwAuthenticate  : " + response.Headers.WwwAuthenticate.FirstOrDefault().ToString());
                respMsg.AppendLine("RequestDate      : " + request.Headers.Date.GetValueOrDefault().ToString("r"));
                respMsg.AppendLine("ServerDate       : " + serverDate.ToString("r"));
                respMsg.AppendLine("DateDifference   : " + (serverDate - request.Headers.Date.GetValueOrDefault()));

                respMsg.AppendLine();

                string username = "";
                if (request.Headers.Contains(HmacApiAuthConfiguration.UsernameHeader)) {
                    username = request.Headers.GetValues(HmacApiAuthConfiguration.UsernameHeader).First();
                }
                string signature = "";
                if (request.Headers.Authorization != null && request.Headers.Authorization.Scheme == HmacApiAuthConfiguration.AuthenticationScheme)
                {
                    signature = request.Headers.Authorization.Parameter;
                }
                string md5 = "";
                string serverMd5 = "";
                long? contentLength = 0;
                if (request.Content != null)
                {
                    contentLength = request.Content.Headers.ContentLength;
                    serverMd5 = Convert.ToBase64String(await MD5Helper.ComputeHash(request.Content)) == "1B2M2Y8AsgTpgAmY7PhCfg==" ? "" : Convert.ToBase64String(await MD5Helper.ComputeHash(request.Content));
                    if (request.Content.Headers.ContentMD5 != null && request.Content.Headers.ContentMD5.Length > 0)
                        md5 = Convert.ToBase64String(request.Content.Headers.ContentMD5);
                }
                bool validRequest = IsRequestValid(request);
                string msgSigRep = _representBuilder.BuildRequestRepresentation(request);
                string serverSignature = _sigCalc.Signature(_secretRepo.GetSecretForUser(username), msgSigRep);

                respMsg.AppendLine("Auth Details\n");

                respMsg.AppendLine("RequestValid     : " + validRequest.ToString());
                respMsg.AppendLine("Username         : " + username);
                respMsg.AppendLine("ApiKey           : " + _secretRepo.GetSecretForUser(username));
                respMsg.AppendLine("Signature        : " + signature);
                respMsg.AppendLine("ServerSignature  : " + serverSignature);

                respMsg.AppendLine();

                respMsg.AppendLine("Content Details\n");
                
                respMsg.AppendLine("ContentMd5       : " + md5);
                respMsg.AppendLine("ServerContentMd5 : " + serverMd5);
                respMsg.AppendLine("CannonicalRep    :\n" + msgSigRep);

                respMsg.AppendLine("ContentLength    : " + contentLength);
                respMsg.AppendLine("ContentType      : " + request.Content.Headers.ContentType);
                respMsg.AppendLine("ContentMediaType : " + request.Content.Headers.ContentType.MediaType.ToLower());
                respMsg.AppendLine("Content          : \"" + await request.Content.ReadAsStringAsync() + "\"");

                response.Content = new StringContent(respMsg.ToString());
            }

            return response;
        }

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

using System.Net.Http;
using System.Threading.Tasks;

namespace NGY.API.Authentication
{
    /// <summary>
    /// Message handler that calculates the MD5 hash of the final request message content and places the hash in the header of the content before the
    /// request is sent.
    /// </summary>
    public class RequestContentMd5Handler : DelegatingHandler
    {
        /// <inheritDoc/>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            // If there is no content then there is nothing to hash. Skip on to the inner message handler.
            if (request.Content == null)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // Compute and add the MD5 hash to the content header.
            request.Content.Headers.ContentMD5 = await MD5Helper.ComputeHash(request.Content);

            // Call inner message handler for response. Typically, this would be another handler that uses the MD5 hash to create a digital signature.
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

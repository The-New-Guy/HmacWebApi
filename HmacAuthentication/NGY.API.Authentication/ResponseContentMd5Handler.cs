
using System.Net.Http;
using System.Threading.Tasks;

namespace NGY.API.Authentication
{
    /// <summary>
    /// Message handler that calculates the MD5 hash of the final response message content and places the hash in the header of the content before the
    /// response is sent.
    /// </summary>
    public class ResponseContentMd5Handler : DelegatingHandler
    {
        /// <inheritDoc/>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {

            // Call inner message handlers for response before MD5 hashing its content.
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            // MD5 hash the response message content if the request was successful and there is in fact content to be hashed.
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                // Compute and add the MD5 hash to the content header.
                response.Content.Headers.ContentMD5 = await MD5Helper.ComputeHash(response.Content);
            }

            // Return the modified response.
            return response;
        }
    }
}

using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NGY.API.Authentication
{
    /// <summary>
    /// Helper class used to calculate the MD5 hash of API request/response message content.
    /// </summary>
    public class MD5Helper
    {
        /// <summary>
        /// Reads in the content of the given request/response message and computes the MD5 hash.
        /// </summary>
        /// <param name="httpContent">The request content to be hashed.</param>
        /// <returns>The MD5 hash of the given request content.</returns>
        public static async Task<byte[]> ComputeHash(HttpContent httpContent)
        {
            using (MD5 md5 = MD5.Create())
            {
                // Read in the message content.
                var content = await httpContent.ReadAsByteArrayAsync();

                // Compute the MD5 hash.
                byte[] hash = md5.ComputeHash(content);

                // Return the hash.
                return hash;
            }
        }
    }
}

using System;
using System.Security.Cryptography;
using System.Text;

namespace NGY.API.Authentication.HMAC
{
    /// <summary>
    /// Calculates an HMAC signature given a secret key and the data string to be signed.
    /// </summary>
    public class HmacSignatureCalculator : ICalculteSignature
    {
        /// <summary>
        /// Calculates an HMAC signature given a secret key and the data string to be signed. Then returns it as a base64 encoded string.
        /// </summary>
        /// <param name="secret">The string representation of the key to be used for the HMAC signature.</param>
        /// <param name="value">The content to be signed.</param>
        /// <returns>A digitial signature signed with HMAC SHA256 and then base64 encoded.</returns>
        public string Signature(string secret, string value)
        {
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var valueBytes = Encoding.UTF8.GetBytes(value);
            string signature;

            using (var hmac = new HMACSHA256(secretBytes))
            {
                var hash = hmac.ComputeHash(valueBytes);
                signature = Convert.ToBase64String(hash);
            }

            return signature;
        }
    }
}

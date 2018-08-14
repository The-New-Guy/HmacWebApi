using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NGY.API.Authentication.HMAC
{
    /// <summary>
    /// A message handler that will digitially sign the request using a Hash-based Message Authentication Code (HMAC) algorithm.
    /// </summary>
    public class HmacSigningHandler : HttpClientHandler
    {
        // A repository to retrieve user secret keys from.
        private readonly ISecretRepository _secretRepository;

        // A string builder that creates the correct representation of the request to be signed.
        private readonly IBuildMessageRepresentation _representationBuilder;

        // The HMAC calculator that performs the actual hashing algorithm.
        private readonly ICalculteSignature _signatureCalculator;

        /// <summary>
        /// The username of the user making the API request.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Creates a new HmacSigningHandler.
        /// </summary>
        /// <param name="secretRepository">A repository to retrieve user secret keys from.</param>
        /// <param name="representationBuilder">A string builder that creates the correct representation of the request to be signed.</param>
        /// <param name="signatureCalculator">The HMAC calculator that performs the actual hashing algorithm.</param>
        public HmacSigningHandler(ISecretRepository secretRepository, IBuildMessageRepresentation representationBuilder, ICalculteSignature signatureCalculator)
        {
            _secretRepository = secretRepository;
            _representationBuilder = representationBuilder;
            _signatureCalculator = signatureCalculator;
        }

        /// <inheritDoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            // Add custom username header if it doesn't exist already.
            if (!request.Headers.Contains(HmacApiAuthConfiguration.UsernameHeader))
            {
                request.Headers.Add(HmacApiAuthConfiguration.UsernameHeader, Username);
            }

            // Set the Date field in the header.
            request.Headers.Date = DateTimeOffset.Now;

            // Build out the string representation of the request.
            var representation = _representationBuilder.BuildRequestRepresentation(request);

            // Retrieve the user secret key from the secrets repository.
            var secret = _secretRepository.GetSecretForUser(Username);

            // Calculate the digital signature.
            string signature = _signatureCalculator.Signature(secret, representation);

            // Add signature to the header of the request message.
            var header = new AuthenticationHeaderValue(HmacApiAuthConfiguration.AuthenticationScheme, signature);
            request.Headers.Authorization = header;

            // Call inner message handler for response.
            return base.SendAsync(request, cancellationToken);
        }
    }
}

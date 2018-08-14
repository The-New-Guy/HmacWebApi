using System.Net.Http;

namespace NGY.API.Authentication
{
    /// <summary>
    /// Classes implementing this interface will be able to build a string representation of the API request message in a format required by the API
    /// authentication signature.
    /// </summary>
    public interface IBuildMessageRepresentation
    {
        /// <summary>
        /// Builds a string representation of the API request message in a format required by the API authentication signature.
        /// </summary>
        /// <param name="requestMessage">The request message to build a string representation from.</param>
        /// <returns></returns>
        string BuildRequestRepresentation(HttpRequestMessage requestMessage);
    }
}
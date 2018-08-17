namespace NGY.API.Authentication.HMAC
{
    /// <summary>
    /// Class used to store configuration data for API authentication.
    /// </summary>
    public class HmacApiAuthConfiguration
    {
        /// <value>
        /// Custom HTTP header used to hold the username of the account attempting to authenticate.
        /// </value>
        public const string UsernameHeader = "X-ApiAuth-Username";

        /// <value>
        /// Custom authentication scheme used to store authentication digital signature.
        /// </value>
        public const string AuthenticationScheme = "ApiAuth";

        /// <value>
        /// The length of time in minutes that a request may differ in order to be a valid request. Any requests that come in plus or minus this
        /// amount will be invalid.
        /// </value>
        public const int ValidityPeriodInMinutes = 5;

        /// <value>
        /// The default message returned in the error response of any unauthenticated requests.
        /// </value>
        public const string UnauthorizedMessage = "Unauthorized request";

        /// <value>
        /// The list of accepted content media-types. (i.e. application/json, text/html, etc.)
        /// </value>
        public static readonly string[] ValidContentMediaTypes = {
            "application/x-www-form-urlencoded",
            "application/json",
            "text/plain"
        };
    }
}

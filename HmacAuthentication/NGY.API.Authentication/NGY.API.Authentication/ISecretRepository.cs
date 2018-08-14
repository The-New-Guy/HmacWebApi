namespace NGY.API.Authentication
{
    /// <summary>
    /// Classes implementing this interface will be able provide secret key lookups for API users.
    /// </summary>
    public interface ISecretRepository
    {
        /// <summary>
        /// Returns the secret key associated with the given username.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>The API secret key.</returns>
        string GetSecretForUser(string username);
    }
}

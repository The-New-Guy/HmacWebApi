namespace NGY.API.Authentication
{
    /// <summary>
    /// Classes implementing this interface will be able calculate a digital signature used for API authentication.
    /// </summary>
    public interface ICalculteSignature
    {
        /// <summary>
        /// Calculates a digital signature used for API authentication.
        /// </summary>
        /// <param name="secret">The secret key used in the signature calculation.</param>
        /// <param name="value">The content to be signed.</param>
        /// <returns>The digital signature.</returns>
        string Signature(string secret, string value);
    }
}
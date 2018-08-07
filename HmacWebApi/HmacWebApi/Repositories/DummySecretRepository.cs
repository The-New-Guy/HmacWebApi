using CCI.API.Authentication;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace HmacWebApi
{
    internal class DummySecretRepository : ISecretRepository
    {
        private readonly IDictionary<string, string> userPasswords
            = new Dictionary<string, string>()
                  {
                      {"username","password"}
                  };

        public string GetSecretForUser(string username)
        {
            if (!userPasswords.ContainsKey(username))
            {
                return null;
            }

            var userPassword = userPasswords[username];
            var hashed = ComputeHash(userPassword, new SHA1CryptoServiceProvider());
            return hashed;
        }

        private string ComputeHash(string inputData, HashAlgorithm algorithm)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            byte[] hashed = algorithm.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashed);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XDeploy.Server.Infrastructure
{
    /// <summary>
    /// Provides cryptographic functionality.
    /// </summary>
    public static class Cryptography
    {
        /// <summary>
        /// Computes and returns the SHA-256 hash of the specified string.
        /// </summary>
        public static string ComputeSHA256(string value)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(value));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}

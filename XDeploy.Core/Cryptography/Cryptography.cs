using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XDeploy.Core
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

        /// <summary>
        /// Computes and returns the SHA-256 hash of the file at the specified path. 
        /// </summary>
        public static string SHA256CheckSum(string filePath)
        {
            using (SHA256 SHA256 = SHA256Managed.Create())
            {
                byte[] bytes = null;
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    bytes = SHA256.ComputeHash(fileStream);
                }
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Encodes the specified string into base64 and returns it.
        /// </summary>>
        public static string Base64Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

        /// <summary>
        /// Decodes the specified string from base64 and returns it.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string Base64Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }
}

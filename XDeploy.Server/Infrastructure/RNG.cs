using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XDeploy.Server.Infrastructure
{
    /// <summary>
    /// Represents a random number generator based on the <see cref="RNGCryptoServiceProvider"/> class.
    /// </summary>
    public static class RNG
    {
        [ImmutableObject(true)]
        private static readonly RNGCryptoServiceProvider _Instance = new RNGCryptoServiceProvider();

        private const string Alphabet = "ABCDEFHHIJKLMNOPQRSTUVWXYZabdefghijklmnopqrstuvwxyz";
        private const string Numbers = "0123456789";
        private static string Alphanumerics => Alphabet + Numbers;

        public enum StringType { Numeric, Alphabet, Alphanumeric }

        /// <summary>
        /// Gets a random string.
        /// </summary>
        public static string GetRandomString(int length, StringType type = StringType.Alphanumeric)
        {
            StringBuilder res = new StringBuilder();
            byte[] uintBuffer = new byte[sizeof(uint)];
            while (length-- > 0)
            {
                _Instance.GetBytes(uintBuffer);
                uint num = BitConverter.ToUInt32(uintBuffer, 0);
                switch (type)
                {
                    case StringType.Alphanumeric:
                        res.Append(Alphanumerics[(int)(num % (uint)Alphanumerics.Length)]);
                        break;
                    case StringType.Alphabet:
                        res.Append(Alphabet[(int)(num % (uint)Alphabet.Length)]);
                        break;
                    case StringType.Numeric:
                        res.Append(Numbers[(int)(num % (uint)Numbers.Length)]);
                        break;
                }
            }
            return res.ToString();
        }
    }
}

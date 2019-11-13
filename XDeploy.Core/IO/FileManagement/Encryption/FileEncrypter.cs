using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Core.IO.FileManagement
{
    /// <summary>
    /// Represents a file encrypter.
    /// </summary>
    public static class FileEncrypter
    {
        /// <summary>
        /// Encrypts a file using the specified algorithm and key.
        /// </summary>
        /// <param name="fileBytes">The source file bytes.</param>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="key">The encryption key.</param>
        /// <returns>The encrypted file bytes</returns>
        public static byte[] EncryptFile(byte[] fileBytes, EncryptionAlgorithm algorithm, string key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Decrypts a file using the specified algorithm and key.
        /// </summary>
        /// <param name="fileBytes">The source file bytes.</param>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="key">The encryption key.</param>
        /// <returns>The encrypted file bytes</returns>
        public static byte[] DecryptFile(byte[] fileBytes, EncryptionAlgorithm algorithm, string key)
        {
            throw new NotImplementedException();
        }
    }
}

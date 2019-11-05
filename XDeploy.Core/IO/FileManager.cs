using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XDeploy.Core.IO
{
    /// <summary>
    /// Represents a file manager.
    /// </summary>
    public class FileManager
    {
        private readonly string _baseLocation;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileManager"/> class.
        /// </summary>
        /// <param name="baseLocation">The base location.</param>
        public FileManager(string baseLocation)
        {
            _baseLocation = baseLocation;
            Directory.CreateDirectory(_baseLocation);
        }

        /// <summary>
        /// Determines whether a file exists at the specified location.
        /// </summary>
        public bool HasFile(string relativePath) => HasFile(relativePath, null);

        /// <summary>
        /// Determines whether a file with the specified SHA-256 checksum exists at the specified location.
        /// </summary>
        public bool HasFile(string relativePath, string sha256checksum)
        {
            var path = Path.Join(_baseLocation, relativePath);
            return File.Exists(path) && ((sha256checksum != null) ? Cryptography.SHA256CheckSum(path) == sha256checksum : true);
        }

        /// <summary>
        /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
        /// </summary>
        public byte[] GetFileBytes(string relativePath) => File.ReadAllBytes(Path.Join(_baseLocation, relativePath));

        /// <summary>
        /// Gets the a file's checksum and bytes.
        /// </summary>
        public (string Checksum, byte[] Bytes) GetFileChecksumAndBytes(string relativePath)
        {
            var path = Path.Join(_baseLocation, relativePath);
            return (Cryptography.SHA256CheckSum(path), File.ReadAllBytes(path));
        }

        public void WriteFile(string relativePath, Stream stream, int length)
        {
            var dir = Path.Combine(_baseLocation, Path.Combine(relativePath.Split(Path.DirectorySeparatorChar)[..^1]));
            Directory.CreateDirectory(dir);
            using (var fs = System.IO.File.Create(Path.Combine(_baseLocation, relativePath)))
            {
                byte[] buffer = null;
                var requestStream = stream;
                using (var reader = new BinaryReader(requestStream))
                {
                    buffer = reader.ReadBytes(length);
                }
                fs.Write(buffer, 0, buffer.Length);
                fs.Close();
            }
        }
    }
}

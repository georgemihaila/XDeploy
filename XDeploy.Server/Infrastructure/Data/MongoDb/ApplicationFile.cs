using Microsoft.Extensions.Hosting.Internal;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XDeploy.Core;
using FileInfo = XDeploy.Core.IO.FileInfo;

namespace XDeploy.Server.Infrastructure.Data.MongoDb
{
    /// <summary>
    /// Represents a container for an application file.
    /// </summary>
    public class ApplicationFile : IMongoDbIndex
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationFile"/> class.
        /// </summary>
        public ApplicationFile()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationFile"/> class.
        /// </summary>
        public ApplicationFile(string applicationID, string fileName, byte[] fileBytes, bool encrypted = false)
        {
            ApplicationID = applicationID;
            Filename = fileName;
            FileBytes = fileBytes;
            SHA256Checksum = Cryptography.ComputeSHA256(fileBytes);
            Encrypted = encrypted;
            LastModified = DateTime.Now;
        }

        /// <summary>
        /// Gets or sets the file bytes.
        /// </summary>
        public byte[] FileBytes { get; set; }

        /// <summary>
        /// Gets or sets the application ID.
        /// </summary>
        public string ApplicationID { get; set; }

        /// <summary>
        /// Gets or sets the SHA-256 checksum of the file.
        /// </summary>
        public string SHA256Checksum { get; set; }

        /// <summary>
        /// Gets or sets the time the file was last modified.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ApplicationFile"/> is encrypted.
        /// </summary>
        public bool Encrypted { get; set; }

        /// <summary>
        /// Gets or sets the object's ID (field used by MongoDb).
        /// </summary>
        public BsonObjectId _id { get; set; }

        /// <summary>
        /// Performs an explicit conversion from <see cref="ApplicationFile"/> to <see cref="BsonDocument"/>.
        /// </summary>
        /// <param name="applicationFile">The application file.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator BsonDocument(ApplicationFile applicationFile) => new BsonDocument() 
        {
            { "_id", new BsonObjectId(ObjectId.GenerateNewId()) },
            { "applicationID", applicationFile.ApplicationID },
            { "sha256Checksum", applicationFile.SHA256Checksum },
            { "fileName", applicationFile.Filename },
            { "fileBytes", applicationFile.FileBytes },
            { "lastModified", applicationFile.LastModified },
            { "encrypted", applicationFile.Encrypted }
        };

        /// <summary>
        /// Performs an explicit conversion from <see cref="ApplicationFile"/> to <see cref="FileInfo"/>.
        /// </summary>
        /// <param name="applicationFile">The application file.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator FileInfo(ApplicationFile applicationFile) => new FileInfo()
        {
            LastModified = applicationFile.LastModified,
            SHA256CheckSum = applicationFile.SHA256Checksum,
            Name = applicationFile.Filename
        };
    }
}

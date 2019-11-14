using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using XDeploy.Core;
using XDeploy.Core.IO;
using System.Linq;
using FileInfo = XDeploy.Core.IO.FileInfo;

namespace XDeploy.Server.Infrastructure.Data.MongoDb
{
    /// <summary>
    /// Represents a MongoDb file manager.
    /// </summary>
    public class MongoDbFileManager
    {
        private readonly MongoClient _client;
        private IMongoDatabase _database;
        private IMongoCollection<ApplicationFile> _fileCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbFileManager" /> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="databaseName">The name of the database storing files.</param>
        /// <param name="encrypter">The encrypter.</param>
        /// <exception cref="ArgumentNullException">connectionString</exception>
        public MongoDbFileManager(string connectionString, string databaseName)
        {
            if (connectionString is null)
                throw new ArgumentNullException(nameof(connectionString));

            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(databaseName);
            _fileCollection = _database.GetCollection<ApplicationFile>("fs.files");
        }

        #region Create

        /// <summary>
        /// Inserts the or updates a file.
        /// </summary>
        /// <param name="applicationID">The application ID.</param>
        /// <param name="filename">The file name.</param>
        /// <param name="fileBytes">The file bytes.</param>
        /// <returns>A result indicating the created or modified object's ID.</returns>
        public async Task InsertOrUpdateFileAsync(string applicationID, string filename, byte[] fileBytes)
        {
            var file = new ApplicationFile(applicationID, filename, fileBytes);
            var filter = (FilterDefinition<ApplicationFile>)(x => x.SHA256Checksum == file.SHA256Checksum && x.ApplicationID == applicationID && x.Filename == filename);
            if (await _fileCollection.CountDocumentsAsync(filter) == 1)
            {
                //Update
                var res = _fileCollection.ReplaceOne(filter, file);
            }
            else
            {
                //Insert
                var document = (BsonDocument)file;
                _fileCollection.InsertOne(file);
            }
        }

        #endregion

        #region Read

        /// <summary>
        /// Determines whether this <see cref="MongoDbFileManager"/> has the specified file.
        /// </summary>
        /// <param name="applicationID">The application ID.</param>
        /// <param name="filename">The filename.</param>
        public async Task<bool> HasFileAsync(string applicationID, string filename)
        {
            var filter = (FilterDefinition<ApplicationFile>)(x => x.ApplicationID == applicationID && x.Filename == filename);
            return await _fileCollection.CountDocumentsAsync(filter) == 1;
        }

        /// <summary>
        /// Determines whether this <see cref="MongoDbFileManager" /> has the specified file.
        /// </summary>
        /// <param name="applicationID">The application ID.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="sha256Checksum">The SHA-256 checksum of the file.</param>
        public async Task<bool> HasFileAsync(string applicationID, string filename, string sha256Checksum)
        {
            var filter = (FilterDefinition<ApplicationFile>)(x => x.SHA256Checksum == sha256Checksum && x.ApplicationID == applicationID && x.Filename == filename);
            return await _fileCollection.CountDocumentsAsync(filter) == 1;
        }

        /// <summary>
        /// Gets all stored files for a specific application.
        /// </summary>
        public async Task<IEnumerable<FileInfo>> GetAllFilesAsync(string applicationID)
        {
            var result = new List<FileInfo>();
            var filter = (FilterDefinition<ApplicationFile>)(x => x.ApplicationID == applicationID);
            foreach (var file in (await _fileCollection.FindAsync(filter)).ToList())
            {
                result.Add((FileInfo)file);
            }
            return result;
        }

        /// <summary>
        /// Gets the file bytes for an application file.
        /// </summary>
        public async Task<byte[]> GetFileBytesAsync(string applicationID, string filename)
        {
            var filter = (FilterDefinition<ApplicationFile>)(x => x.ApplicationID == applicationID && x.Filename == filename);
            return (await _fileCollection.FindAsync(filter)).First().FileBytes;
        }

        #endregion

        #region Update

        #endregion

        #region Delete

        /// <summary>
        /// Tries to delete a file.
        /// </summary>
        /// <returns>A value indicating whether the operation was successful.</returns>
        public async Task<bool> TryDeleteFileAsync(string applicationID, string filename)
        {
            var filter = (FilterDefinition<ApplicationFile>)(x => x.ApplicationID == applicationID && x.Filename == filename);
            if (await _fileCollection.CountDocumentsAsync(filter) == 1)
            {
                _fileCollection.DeleteOne(filter);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to delete a file.
        /// </summary>
        /// <returns>A value indicating whether the operation was successful.</returns>
        public async Task<bool> TryDeleteFileAsync(string applicationID, string filename, string sha256Checksum)
        {
            var filter = (FilterDefinition<ApplicationFile>)(x => x.SHA256Checksum == sha256Checksum && x.ApplicationID == applicationID && x.Filename == filename);
            if (await _fileCollection.CountDocumentsAsync(filter) == 1)
            {
                _fileCollection.DeleteOne(filter);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deletes all files for an application.
        /// </summary>
        public async Task DeleteAllFilesAsync(string applicationID)
        {
            var filter = (FilterDefinition<ApplicationFile>)(x => x.ApplicationID == applicationID);
            await _fileCollection.DeleteManyAsync(filter);
        }

        /// <summary>
        /// Deletes all encrypted files for an application.
        /// </summary>
        public async Task DeleteAllEncryptedFilesAsync(string applicationID)
        {
            var filter = (FilterDefinition<ApplicationFile>)(x => x.ApplicationID == applicationID && x.Encrypted);
            await _fileCollection.DeleteManyAsync(filter);
        }

        /// <summary>
        /// Deletes all non-encrypted files for an application.
        /// </summary>
        public async Task DeleteAllNonEncryptedFilesAsync(string applicationID)
        {
            var filter = (FilterDefinition<ApplicationFile>)(x => x.ApplicationID == applicationID && !x.Encrypted);
            await _fileCollection.DeleteManyAsync(filter);
        }

        #endregion
    }
}

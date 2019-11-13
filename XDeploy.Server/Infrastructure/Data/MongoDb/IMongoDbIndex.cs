using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XDeploy.Server.Infrastructure.Data.MongoDb
{
    /// <summary>
    /// Represents a MongoDb index
    /// </summary>
    public interface IMongoDbIndex
    {
        /// <summary>
        /// Gets or sets the object's ID (field used by MongoDb).
        /// </summary>
        public BsonObjectId _id { get; set; }
    }
}

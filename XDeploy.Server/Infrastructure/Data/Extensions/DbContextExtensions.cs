using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace XDeploy.Server.Infrastructure.Data.Extensions
{
    /// <summary>
    /// Provides extension methods for the current database context.
    /// </summary>
    public static class DbContextExtensions
    {
        /// <summary>
        /// Determines whether at least one application with the specified id exists in the source collection.
        /// </summary>
        public static bool Exists(this IEnumerable<Application> apps, string id) => apps.Count(x => x.ID == id) > 0;

        /// <summary>
        /// Returns the first <see cref="Application"/> in the collection that is identified by the spcified ID.
        /// </summary>
        public static Application FirstByID(this IEnumerable<Application> apps, string id) => apps.First(x => x.ID == id);

        /// <summary>
        /// Determines whether the specified application's owner matches the one provided.
        /// </summary>
        public static bool HasOwner(this Application app, ClaimsPrincipal owner) => app.OwnerEmail == owner.Identity.Name;
    }
}

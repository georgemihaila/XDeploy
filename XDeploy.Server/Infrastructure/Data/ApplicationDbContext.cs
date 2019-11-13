using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace XDeploy.Server.Infrastructure.Data
{
    /// <summary>
    /// Represents the main application db context.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {

        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeploymentJob>()
                .HasMany(x => x.ExpectedFiles)
                .WithOne(x => x.ParentJob);
        }

        /// <summary>
        /// Gets or sets the API keys.
        /// </summary>
        public DbSet<APIKey> APIKeys { get; set; }

        /// <summary>
        /// Gets or sets the applications.
        /// </summary>
        public DbSet<Application> Applications { get; set; }

        /// <summary>
        /// Determines whether the specified user has at least one API key.
        /// </summary>
        public bool HasAPIKeys(ClaimsPrincipal claims) => APIKeys.Count(x => x.UserEmail == claims.Identity.Name) > 0;
    }
}

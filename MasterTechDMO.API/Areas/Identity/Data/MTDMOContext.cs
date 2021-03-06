﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MasterTechDMO.API.Areas.Identity.Data
{
    public class MTDMOContext : IdentityDbContext<DMOUsers>
    {
        public MTDMOContext(DbContextOptions<MTDMOContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        public DbSet<DMOOrgRoles> DMOOrgRoles { get; set; }

        public DbSet<DMOUserFriendList> DMOUserFriendList { get; set; }
        public DbSet<DMOTemplateData> DMOTemplateData { get; set; }
        public DbSet<DMOTaskScheduler> DMOTaskScheduler { get; set; }

    }
}

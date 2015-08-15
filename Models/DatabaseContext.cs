using System;
using System.Data.Entity;

namespace CollapsedToto
{
    [DbConfigurationType(typeof(MySql.Data.Entity.MySqlEFConfiguration))]
    public class DatabaseContext :  DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserRound> UserRounds { get; set; }
        public DbSet<RoundResult> RoundResults { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}


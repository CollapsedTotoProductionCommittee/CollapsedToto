using System;
using System.Data.Entity;

namespace CollapsedToto
{
    public class DbInitializer<TContext> : IDatabaseInitializer<TContext>
        where TContext : DbContext
    {
        #region IDatabaseInitializer implementation

        public void InitializeDatabase(TContext context)
        {
            if (context.Database.Exists())
            {
                try
                {
                    if (!context.Database.CompatibleWithModel(true))
                    {
                        context.Database.Delete();
                    }
                }
                catch
                {
                    context.Database.Delete();
                }
            }

            context.Database.CreateIfNotExists();
        }

        #endregion
    }
}


using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(IConfigurationRegistry), typeof(ConfigurationRegistry))]
namespace JudgeWeb.Domains.Problems
{
    public class ConfigurationRegistry :
        IConfigurationRegistry
    {
        public DbContext Context { get; }

        DbSet<Configure> Configurations => Context.Set<Configure>();

        public ConfigurationRegistry(DbContextAccessor context)
        {
            Context = context;
        }

        public Task<List<Configure>> ListPublicAsync()
        {
            return Configurations
                .Where(c => c.Public >= 0)
                .ToListAsync();
        }

        public Task UpdateValueAsync(string name, string newValue)
        {
            return Configurations
                .Where(c => c.Name == name)
                .BatchUpdateAsync(c => new Configure { Value = newValue });
        }

        public Task<Configure> FindAsync(string config)
        {
            return Configurations
                .Where(c => c.Name == config)
                .SingleOrDefaultAsync();
        }

        public Task<List<Configure>> GetAsync(string name)
        {
            IQueryable<Configure> confQuery = Configurations;
            if (name != null) confQuery = confQuery.Where(c => c.Name == name);
            return confQuery.ToListAsync();
        }
    }
}

using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementFacade :
        IConfigurationRegistry
    {
        public IConfigurationRegistry Configurations => this;

        Task<List<Configure>> IConfigurationRegistry.ListPublicAsync()
        {
            return Context.Set<Configure>()
                .Where(c => c.Public >= 0)
                .ToListAsync();
        }

        Task IConfigurationRegistry.UpdateValueAsync(string name, string newValue)
        {
            return Context.Set<Configure>()
                .Where(c => c.Name == name)
                .BatchUpdateAsync(c => new Configure { Value = newValue });
        }

        Task<Configure> IConfigurationRegistry.FindAsync(string config)
        {
            return Context.Set<Configure>()
                .Where(c => c.Name == config)
                .SingleOrDefaultAsync();
        }

        Task<List<Configure>> IConfigurationRegistry.GetAsync(string name)
        {
            IQueryable<Configure> confQuery = Context.Set<Configure>();
            if (name != null) confQuery = confQuery.Where(c => c.Name == name);
            return confQuery.ToListAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
#pragma warning disable CS0618

namespace JudgeWeb.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseLoggerFactory(new LoggerFactory().AddConsole());

            // For dotnet cli users: 
            //   dotnet ef migrations add InitialForMyDbContext
            //   dotnet ef database update

            // For powershell users:
            //   Add-Migration -Name InitialForMyDbContext
            //   Update-Database

            // You can change UseSqlServer to your dbs, e.g. MySQL

            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;" +
                "Database=aspnet-JudgeWeb;" +
                "Trusted_Connection=True;" +
                "MultipleActiveResultSets=true");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}

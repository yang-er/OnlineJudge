using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace JudgeWeb.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            optionsBuilder.UseLoggerFactory(loggerFactory);

            // For dotnet cli users: 
            //   dotnet ef migrations add InitialForMyDbContext
            //   dotnet ef database update

            // For powershell users:
            //   Add-Migration -Name InitialForMyDbContext
            //   Update-Database

            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;" +
                "Database=aspnet-JudgeWeb;" +
                "Trusted_Connection=True;" +
                "MultipleActiveResultSets=true");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}

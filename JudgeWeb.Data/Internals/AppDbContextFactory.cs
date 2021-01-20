using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JudgeWeb.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // For dotnet cli users: 
            //   dotnet ef migrations add InitialForMyDbContext
            //   dotnet ef database update

            // For powershell users:
            //   Add-Migration -Name InitialForMyDbContext
            //   Update-Database

            // You can change UseSqlServer to your dbs, e.g. MySQL
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseMySQL("server=localhost;user id=oj;database=acm3xylab;character set=utf-8;TreatTinyAsBoolean=true");
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Text;

namespace JudgeWeb
{
    public class Program
    {
        public static IHost Current { get; private set; }

        public static void Main(string[] args)
        {
            Culture.SetCultureInfo("zh-CN");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            (Current = CreateWebHostBuilder(args).Build()).Run();
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(builder =>
                    builder.UseStartup<Startup>());
    }
}

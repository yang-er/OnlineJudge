using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;
using System.Text;

namespace JudgeWeb
{
    public class Program
    {
        public static IWebHost Current { get; private set; }

        public static void Main(string[] args)
        {
            Culture.SetCultureInfo("zh-CN");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            (Current = CreateWebHostBuilder(args).Build()).Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()
                .UseStartup<Startup>();
    }
}

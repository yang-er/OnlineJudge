using System;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace JudgeWeb.Migration
{
    public class Program
    {
        public static IHost Current { get; private set; }

        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Current = CreateHostBuilder(args).Build();
            Current.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Startup.CreateDefaultBuilder(args)
                .UseConsoleLifetime();
    }
}

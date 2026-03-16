using System;
using System.Windows;
using Velopack;
using GestLog.Services.Core;

namespace GestLog;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("--provision-bootstrap", StringComparison.OrdinalIgnoreCase))
        {
            var environment = Environment.GetEnvironmentVariable("GESTLOG_ENVIRONMENT") ?? "Production";
            var result = BootstrapProvisioningService
                .EnsureBootstrapAndMigrateAsync(environment, AppContext.BaseDirectory)
                .GetAwaiter()
                .GetResult();

            Console.WriteLine($"BootstrapResult_Message={result.Message}");
            Console.WriteLine($"BootstrapResult_Path={result.BootstrapPath}");
            Console.WriteLine($"BootstrapResult_Created={result.BootstrapCreated}");
            Console.WriteLine($"BootstrapResult_Updated={result.BootstrapUpdated}");
            Console.WriteLine($"BootstrapResult_EnvCreated={result.EnvironmentVariablesCreated}");
            Console.WriteLine($"BootstrapResult_EnvUpdated={result.EnvironmentVariablesUpdated}");
            Console.WriteLine($"BootstrapResult_Skipped={result.Skipped}");
            return;
        }

#if !DEBUG
        VelopackApp.Build().Run();
#endif

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}

using System;
using System.Windows;
using Velopack;

namespace GestLog;

public static class Program
{
    [STAThread]
    public static void Main()
    {
#if !DEBUG
        VelopackApp.Build().Run();
#endif

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}

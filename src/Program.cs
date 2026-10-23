using System;
using System.Threading;
using System.Windows;

[assembly: System.Reflection.AssemblyTitle("Pupitre")]
[assembly: System.Reflection.AssemblyProduct("Pupitre")]
[assembly: System.Reflection.AssemblyDescription("Organiseur de bureau et suivi de tâches, 100 % hors ligne")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName = ".NET Framework 4.8")]

namespace Pupitre
{
    internal static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            bool created;
            using (new Mutex(true, "Local\\Pupitre.SingleInstance", out created))
            {
                if (!created)
                {
                    // instance déjà lancée : « --parametres » lui demande d'ouvrir ses paramètres
                    if (Array.Exists(args, x => string.Equals(x, "--parametres", StringComparison.OrdinalIgnoreCase)))
                        try { using (var ev = EventWaitHandle.OpenExisting("Local\\Pupitre.OpenSettings")) ev.Set(); } catch { }
                    // « --quitter » (mise à jour) : l'instance en cours enregistre tout et se ferme
                    if (Array.Exists(args, x => string.Equals(x, "--quitter", StringComparison.OrdinalIgnoreCase)))
                        try { using (var ev = EventWaitHandle.OpenExisting("Local\\Pupitre.Quit")) ev.Set(); } catch { }
                    return 0;
                }
                if (Array.Exists(args, x => string.Equals(x, "--quitter", StringComparison.OrdinalIgnoreCase))) return 0;   // rien à fermer
                var app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.Resources.MergedDictionaries.Add(Theme.Create());
                var controller = new Controller(app);
                app.DispatcherUnhandledException += (s, e) => { Log.Fatal(e.Exception); e.Handled = true; };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => { Log.Fatal(e.ExceptionObject as Exception ?? new Exception("exception non gérée")); controller.OnCrash(); };
                bool openSettings = Array.Exists(args, x => string.Equals(x, "--parametres", StringComparison.OrdinalIgnoreCase));
                controller.AutoStarted = Array.Exists(args, x => string.Equals(x, "--demarrage", StringComparison.OrdinalIgnoreCase));
                app.Startup += (s, e) => { controller.Start(); if (openSettings) controller.OpenSettings(); };
                app.Exit += (s, e) => controller.Dispose();
                return app.Run();
            }
        }
    }
}

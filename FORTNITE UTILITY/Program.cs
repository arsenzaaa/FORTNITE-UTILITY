using System;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace FortniteUtility;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FortniteUtility",
            "last-crash.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        void Log(Exception ex)
        {
            try
            {
                var activeForm = Form.ActiveForm;
                var activeControl = activeForm?.ActiveControl;
                var extra = activeForm == null
                    ? "ActiveForm: <none>"
                    : $"ActiveForm: {activeForm.Name} ({activeForm.GetType().Name}), ActiveControl: {activeControl?.Name ?? "<none>"} ({activeControl?.GetType().Name ?? "n/a"})";

                File.WriteAllText(
                    logPath,
                    $"{DateTime.Now:O}{Environment.NewLine}{extra}{Environment.NewLine}{ex}");
            }
            catch
            {
                // swallow logging failures
            }
        }

        Application.ThreadException += (_, e) => Log(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex) Log(ex);
        };

        ApplicationConfiguration.Initialize();
        bool softCheck = args.Any(a => string.Equals(a, "soft", StringComparison.OrdinalIgnoreCase));
        Application.Run(new MainForm(softCheck));
    }
}

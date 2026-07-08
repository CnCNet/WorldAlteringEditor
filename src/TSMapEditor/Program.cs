using System;
using System.Linq;
using System.Windows.Forms;
using TSMapEditor.Initialization;
using TSMapEditor.Rendering;

namespace TSMapEditor
{
    static class Program
    {
        public static string[] args;

        /// <summary>
        /// When set, the editor was launched in headless random-map-generation mode
        /// (via the --generate-map command line flag) instead of the interactive GUI.
        /// </summary>
        public static HeadlessRmgOptions HeadlessRmgOptions { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Program.args = args;

            if (args.Any(a => a.Equals("--generate-map", StringComparison.OrdinalIgnoreCase)))
            {
                HeadlessRmgOptions = HeadlessRmgOptions.ParseArgs(args);

                string validationError = HeadlessRmgOptions.Validate();
                if (validationError != null)
                {
                    Console.Error.WriteLine("Invalid --generate-map arguments: " + validationError);
                    Environment.Exit(1);
                    return;
                }
            }

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Environment.CurrentDirectory = Application.StartupPath.Replace('\\', '/');
            new GameClass().Run();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException((Exception)e.ExceptionObject);
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        private static void HandleException(Exception ex)
        {
            MessageBox.Show("The map editor failed to launch.\r\n\r\nReason: " + ex.Message + "\r\n\r\n Stack trace: " + ex.StackTrace);
        }

        public static void DisableExceptionHandler()
        {
            Application.ThreadException -= Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        }
    }
}

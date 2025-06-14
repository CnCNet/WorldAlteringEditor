using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MapEditorLauncher
{
    class Program
    {
        // We have to do this because of .NET 7 limitations.
        // If the user does not have .NET 7 installed, the program tells
        // the user to install it.

        // However, if your application is a console application, the
        // error message is written to the standard output instead
        // of showing a graphical form. For the average user,
        // this is a no-go.

        // That can be fixed by making your application a Windows application,
        // and the user gets a nice graphical error for missing .NET 7.
        // However, Windows applications don't have a platform-agnostic way to
        // open the console.

        // So the best thing we can do is make our application a Windows
        // application, and call AllocConsole to show the console window.
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        static void Main(string[] args)
        {
            AllocConsole();

            Environment.CurrentDirectory = AppContext.BaseDirectory;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Console.WriteLine("DTA Scenario Editor Launcher");
            Console.WriteLine("By Rampastring");
            Console.WriteLine("http://rampastring.net");
            Console.WriteLine();

            new UpdaterLink().Run();

            Console.WriteLine("Launching scenario editor.");
            Process.Start(new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "Editor", "WorldAlteringEditor.exe"))
                {
                    UseShellExecute = false,
                    Arguments = "\"" + string.Join(" ", args) + "\""
                }
            );
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string exceptLogPath = Environment.CurrentDirectory + "/LauncherCrash.log";
            Exception ex = (Exception)e.ExceptionObject;
            File.Delete(exceptLogPath);

            StringBuilder sb = new StringBuilder();

            Console.WriteLine("Unhandled exception! @ " + DateTime.Now.ToLongTimeString(), sb, exceptLogPath);
            LogLineGenerate("Message: " + ex.Message, sb, exceptLogPath);
            LogLineGenerate("Stack trace: " + ex.StackTrace, sb, exceptLogPath);

            if (ex.InnerException != null)
            {
                LogLineGenerate("***************************", sb, exceptLogPath);
                LogLineGenerate("InnerException information:", sb, exceptLogPath);
                LogLineGenerate("Message: " + ex.InnerException.Message, sb, exceptLogPath);
                LogLineGenerate("Stack trace: " + ex.InnerException.StackTrace, sb, exceptLogPath);
            }

            Console.WriteLine("An error was encountered while starting or updating the map editor.");
            Console.WriteLine("If the problem persists, please contact the developers for support.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();

            Environment.Exit(255);
        }

        private static void LogLineGenerate(string text, StringBuilder sb, string exceptLogPath)
        {
            sb.Append(text + Environment.NewLine);
            Console.WriteLine(text + Environment.NewLine);
        }
    }
}

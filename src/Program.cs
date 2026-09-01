using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace TiaOpennessLibraryInspectorDemo
{
    internal static class Program
    {
        private static string _tiaApiDirectory = string.Empty;

        [STAThread]
        private static void Main()
        {
            _tiaApiDirectory = FindTiaApiDirectory();
            if (_tiaApiDirectory.Length == 0)
            {
                MessageBox.Show(
                    "TIA Portal V21 Openness was not found. Install TIA Portal V21 with " +
                    "Openness, or set TIA_PORTAL_V21_PUBLIC_API to the local net48 PublicAPI folder.",
                    "TIA Portal V21 Openness required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveSiemensAssembly;
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(new TiaLibraryInspector()));
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    exception.ToString(),
                    "Application startup failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveSiemensAssembly;
            }
        }

        private static string FindTiaApiDirectory()
        {
            var candidates = new List<string>();
            string configured = Environment.GetEnvironmentVariable("TIA_PORTAL_V21_PUBLIC_API");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                candidates.Add(configured);
            }

            string programFiles = Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                candidates.Add(Path.Combine(
                    programFiles,
                    "Siemens",
                    "Automation",
                    "Portal V21",
                    "PublicAPI",
                    "V21",
                    "net48"));
            }

            foreach (string candidate in candidates)
            {
                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(candidate);
                }
                catch
                {
                    continue;
                }

                if (File.Exists(Path.Combine(fullPath, "Siemens.Engineering.Base.dll")))
                {
                    return fullPath;
                }
            }

            return string.Empty;
        }

        private static Assembly ResolveSiemensAssembly(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;
            if (string.IsNullOrWhiteSpace(name) ||
                !name.StartsWith("Siemens.Engineering", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string path = Path.Combine(_tiaApiDirectory, name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }
    }
}

using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Net;

namespace ElvUI_Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            // https://git.tukui.org/elvui/elvui/repository/development/archive.zip
            // https://git.tukui.org/elvui/elvui/repository/master/archive.zip

            try
            {
                string executablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar;

                string addonPath = "Interface" + Path.DirectorySeparatorChar + "Addons" + Path.DirectorySeparatorChar;
                string installPath = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Blizzard Entertainment\\World of Warcraft", "InstallPath", null);

                if (installPath == null)
                {
                    Console.WriteLine("Unable to find the game path, exiting ...");
                    return;
                }

                Console.WriteLine("Using game in: {0}", installPath);

                if (!Directory.Exists(installPath + addonPath))
                {
                    Console.WriteLine("Addons path not found, exiting ...");
                    return;
                }

                clean(executablePath);

                Console.WriteLine("Downloading new files ...");
                WebClient webClient = new WebClient();
                webClient.DownloadFile("https://git.tukui.org/elvui/elvui/repository/master/archive.zip", executablePath + "elvui-latest.zip");

                List<string> elvuiDirectories = new List<string>()
                {
                    "ElvUI",
                    "ElvUI_OptionsUI"
                };

                foreach (string directory in elvuiDirectories)
                {
                    if (Directory.Exists(installPath + addonPath + directory))
                    {
                        Console.WriteLine("Addon {0} found, deleting ...", directory);
                        Directory.Delete(installPath + addonPath + directory, true);
                    }
                }

                Console.WriteLine("Unzipping ...");
                ZipFile.ExtractToDirectory(executablePath + "elvui-latest.zip", executablePath + "tmp");

                Console.WriteLine("Copying new files ...");

                string tmpDir = "";
                List<string> dirs = new List<string>(Directory.EnumerateDirectories(executablePath + "tmp"));
                foreach (string dir in dirs)
                {
                    tmpDir = $"{dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1)}";
                    break;
                }

                dirs = new List<string>(Directory.EnumerateDirectories(executablePath + "tmp" + Path.DirectorySeparatorChar + tmpDir));
                foreach (string dir in dirs)
                {
                    Console.WriteLine(installPath + addonPath + $"{dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1)}");
                    Copy(dir, installPath + addonPath + $"{dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1)}");
                }

                Console.WriteLine("Cleaning ...");
                clean(executablePath);

                Console.WriteLine("Installation completed !");
            }
            catch(Exception e)
            {
                Console.WriteLine("FATAL ERROR: " + e.Message);
            }

            Console.ReadKey();
        }

        private static void clean(string executablePath)
        {
            if (File.Exists(executablePath + "elvui-latest.zip"))
                File.Delete(executablePath + "elvui-latest.zip");

            if (Directory.Exists(executablePath + "tmp"))
                Directory.Delete(executablePath + "tmp", true);
        }

        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}

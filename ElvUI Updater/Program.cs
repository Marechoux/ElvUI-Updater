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
        enum PathType { ADDONS, ARCHIVE, EXECUTABLE, INTERFACE, INSTALL, TMP };

        static string installPath = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Blizzard Entertainment\\World of Warcraft", "InstallPath", null);

        static void Main(string[] args)
        {
            // https://git.tukui.org/elvui/elvui/repository/development/archive.zip
            // https://git.tukui.org/elvui/elvui/repository/master/archive.zip

            try
            {
                if (installPath == null)
                {
                    Console.WriteLine("Unable to find the game path, exiting ...");
                    return;
                }

                Console.WriteLine("Using game in: {0}", getPath(PathType.INSTALL));

                if (!Directory.Exists(getPath(PathType.ADDONS)))
                {
                    Console.WriteLine("Addons path not found, exiting ...");
                    return;
                }

                clean();

                Console.WriteLine("Which version do you want to install ? [def = m]");
                Console.WriteLine("Master: m");
                Console.WriteLine("Development: d");

                bool notFound = true;
                string downloadUrl = null;
                do
                {
                    ConsoleKeyInfo cki = Console.ReadKey();
                    switch (cki.Key)
                    {
                        case ConsoleKey.Enter:
                        case ConsoleKey.M:
                            downloadUrl = "https://git.tukui.org/elvui/elvui/repository/master/archive.zip";
                            notFound = false;
                            break;
                        case ConsoleKey.D:
                            downloadUrl = "https://git.tukui.org/elvui/elvui/repository/development/archive.zip";
                            notFound = false;
                            break;
                        default:
                            break;
                    }
                } while (notFound);

                Console.WriteLine("");
                Console.WriteLine("Downloading new files ...");
                WebClient webClient = new WebClient();
                webClient.DownloadFile(downloadUrl, getPath(PathType.ARCHIVE));

                List<string> elvuiDirectories = new List<string>()
                {
                    "ElvUI",
                    "ElvUI_OptionsUI"
                };

                foreach (string directory in elvuiDirectories)
                {
                    if (Directory.Exists(getPath(PathType.ADDONS) + directory))
                    {
                        Console.WriteLine("Addon {0} found, deleting ...", directory);
                        Directory.Delete(getPath(PathType.ADDONS) + directory, true);
                    }
                }

                Console.WriteLine("Unzipping ...");
                ZipFile.ExtractToDirectory(getPath(PathType.ARCHIVE), getPath(PathType.TMP));

                Console.WriteLine("Copying new files ...");

                string tmpDir = "";
                List<string> dirs = new List<string>(Directory.EnumerateDirectories(getPath(PathType.TMP)));
                foreach (string dir in dirs)
                {
                    tmpDir = $"{dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1)}";
                    break;
                }

                dirs = new List<string>(Directory.EnumerateDirectories(getPath(PathType.TMP) + Path.DirectorySeparatorChar + tmpDir));
                foreach (string dir in dirs)
                {
                    if (dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1)[0] == '.')
                        continue;

                    Console.WriteLine(getPath(PathType.ADDONS) + $"{dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1)}");
                    Copy(dir, getPath(PathType.ADDONS) + $"{dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1)}");
                }

                Console.WriteLine("Cleaning ...");
                clean();

                Console.WriteLine("Installation completed !");
            }
            catch(Exception e)
            {
                Console.WriteLine("FATAL ERROR: " + e.Message);
            }

            Console.ReadKey();
        }

        private static void clean()
        {
            if (File.Exists(getPath(PathType.ARCHIVE)))
                File.Delete(getPath(PathType.ARCHIVE));

            if (Directory.Exists(getPath(PathType.TMP)))
                Directory.Delete(getPath(PathType.TMP), true);
        }

        static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        static void CopyAll(DirectoryInfo source, DirectoryInfo target)
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

        static string getPath(PathType type)
        {
            switch (type)
            {
                case PathType.ADDONS:
                    return installPath + "Interface" + Path.DirectorySeparatorChar + "Addons" + Path.DirectorySeparatorChar;
                case PathType.ARCHIVE:
                    return installPath + "Interface" + Path.DirectorySeparatorChar + "elvui-latest.zip";
                case PathType.EXECUTABLE:
                    return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar;
                case PathType.INTERFACE:
                    return installPath + "Interface" + Path.DirectorySeparatorChar;
                case PathType.TMP:
                    return installPath + "Interface" + Path.DirectorySeparatorChar + "ElvUI_Update" + Path.DirectorySeparatorChar;
                case PathType.INSTALL:
                default:
                    return installPath;
            }
        }
    }
}

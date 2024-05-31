using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ElvUI_Updater
{
    class Program
    {
        enum PathType { ADDONS, ARCHIVE, EXECUTABLE, INTERFACE, INSTALL, TMP, TOC };

        static string installPath = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\World of Warcraft", "InstallLocation", null);

        static string installedVersion = null;

        static async Task Main(string[] args)
        {
            // https://git.tukui.org/elvui/elvui/repository/development/archive.zip
            // https://git.tukui.org/elvui/elvui/repository/master/archive.zip

            // https://api.tukui.org/v1/addon/elvui

            try
            {
                if (installPath == null)
                {
                    Console.WriteLine("Unable to find the game path, exiting ...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("Using game in: {0}", getPath(PathType.INSTALL));

                if (!Directory.Exists(getPath(PathType.ADDONS)))
                {
                    Console.WriteLine("Addons path not found, exiting ...");
                    Console.ReadKey();
                    return;
                }

                clean();

                try
                {
                    using (StreamReader reader = new StreamReader(getPath(PathType.TOC)))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("## Version:"))
                            {
                                Regex regex = new Regex(@"## Version:\s*v(\d+\.\d+)");
                                Match match = regex.Match(line);
                                if (match.Success)
                                {
                                    installedVersion = match.Groups[1].Value;
                                    break;
                                }
                            }
                        }

                        Console.WriteLine("Installed version: {0}", installedVersion);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error reading toc file: {0}", e.Message);
                }

                try
                {
                    await Addon.LoadFromUrl("https://api.tukui.org/v1/addon/elvui");
                    Console.WriteLine("Last version:      {0}", Addon.Version);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("Request error: {0}", e.Message);
                }
                catch (JsonException e)
                {
                    Console.WriteLine("JSON deserialization error: {0}", e.Message);
                }

                if (installedVersion == Addon.Version)
                {
                    Console.WriteLine("Last version is already installed !");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("Process with the update ? (y, n [def = n])");

                bool proceed = false;

                ConsoleKeyInfo cki = Console.ReadKey();
                switch (cki.Key)
                {
                    case ConsoleKey.Y:
                        proceed = true;
                        break;
                    default:
                        break;
                }

                if (!proceed)
                {
                    return;
                }

                Console.WriteLine("");
                Console.WriteLine("Downloading new files ...");
                WebClient webClient = new WebClient();
                webClient.DownloadFile(Addon.Url, getPath(PathType.ARCHIVE));

                List<string> elvuiDirectories = new List<string>()
                {
                    "ElvUI",
                    "ElvUI_Libraries",
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

                List<string> dirs = new List<string>(Directory.EnumerateDirectories(getPath(PathType.TMP)));
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
                    return installPath + Path.DirectorySeparatorChar + "_retail_" + Path.DirectorySeparatorChar + "Interface" + Path.DirectorySeparatorChar + "Addons" + Path.DirectorySeparatorChar;
                case PathType.ARCHIVE:
                    return installPath + Path.DirectorySeparatorChar + "_retail_" + Path.DirectorySeparatorChar + "Interface" + Path.DirectorySeparatorChar + "elvui-latest.zip";
                case PathType.EXECUTABLE:
                    return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar;
                case PathType.INTERFACE:
                    return installPath + Path.DirectorySeparatorChar + "_retail_" + Path.DirectorySeparatorChar  + "Interface" + Path.DirectorySeparatorChar;
                case PathType.TMP:
                    return installPath + Path.DirectorySeparatorChar + "_retail_" + Path.DirectorySeparatorChar  + "Interface" + Path.DirectorySeparatorChar + "ElvUI_Update" + Path.DirectorySeparatorChar;
                case PathType.TOC:
                    return installPath + Path.DirectorySeparatorChar + "_retail_" + Path.DirectorySeparatorChar + "Interface" + Path.DirectorySeparatorChar + "Addons" + Path.DirectorySeparatorChar + "ElvUI" + Path.DirectorySeparatorChar + "ElvUI_Mainline.toc";
                case PathType.INSTALL:
                default:
                    return installPath;
            }
        }
    }
}

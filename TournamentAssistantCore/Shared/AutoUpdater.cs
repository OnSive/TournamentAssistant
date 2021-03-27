﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TournamentAssistantShared;
using TournamentAssistantShared.SimpleJSON;

namespace TournamentAssistantCore.Shared
{
    class AutoUpdater
    {
        public static string osType = Convert.ToString(Environment.OSVersion);

        //For easy switching if those ever changed
        private static readonly string repoURL = "https://github.com/MatrikMoon/TournamentAssistant/releases/latest";
        private static readonly string repoAPI = "https://api.github.com/repos/MatrikMoon/TournamentAssistant/releases/latest";
        private static readonly string linuxFilename = "TournamentAssistantCore-linux";
        private static readonly string WindowsFilename = "TournamentAssistantCore.exe";
        public static async Task<bool> AttemptAutoUpdate()
        {
            string CurrentFilename;
            if (osType.Contains("Unix")) CurrentFilename = linuxFilename;
            else if (osType.Contains("Windows")) CurrentFilename = WindowsFilename;
            else
            {
                Logger.Error($"AutoUpdater does not support your operating system. Detected Operating system is: {osType}. Supported are: Unix, Windows");
                return false;
            }

            Uri URI = await GetExecutableURI(CurrentFilename);

            if (URI == null)
            {
                Logger.Error($"AutoUpdate resource not found. Please update manually from: {repoURL}");
                return false;
            }

            //Delete any .old executables, if there are any.
            File.Delete($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{CurrentFilename}.old");

            //Rename current executable to .old
            File.Move($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{CurrentFilename}", $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{CurrentFilename}.old");

            //Download new executable
            Logger.Info("Downloading new version...");
            await GetExecutableFromURI(URI, CurrentFilename);
            Logger.Success("New version downloaded sucessfully!");

            //Restart as the new version
            Logger.Info("Attempting to start new version");
            if (CurrentFilename.Contains("linux"))
            {
                //This is pretty hacky, but oh well....
                Process.Start("chmod", $"+x {Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{CurrentFilename}");

                if (!File.Exists($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Update.sh"))
                {
                    Logger.Info("Downloading update script...");
                    Uri.TryCreate("https://raw.githubusercontent.com/Arimodu/TAUpdateScript/main/Update.sh", 0, out Uri resultUri)
                    await GetExecutableFromURI(resultUri, "Update.sh");
                    Logger.Success("Update script downloaded sucessfully!");
                    Process.Start("chmod", $"+x {Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Update.sh");
                }
                Process.Start("bash", $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Update.sh");
                Logger.Success("Application updated succesfully!!");
                return true;
            }
            using (Process newVersion = new())
            {
                newVersion.StartInfo.UseShellExecute = true;
                newVersion.StartInfo.CreateNoWindow = false;
                newVersion.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                newVersion.StartInfo.FileName = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{CurrentFilename}";
                newVersion.Start();
            }
            Logger.Success("Application updated succesfully!!");
            return true;
        }

        public static async Task GetExecutableFromURI(Uri URI, string filename)
        {
            WebClient Client = new();
            Client.DownloadProgressChanged += DownloadProgress;
            await Client.DownloadFileTaskAsync(URI, filename);
            Console.WriteLine("\n\n");
            return;            
        }

        private static void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.Write($"\rDownloaded {e.BytesReceived} / {e.TotalBytesToReceive} bytes. {e.ProgressPercentage} % complete...");
        }

        public static async Task<Uri> GetExecutableURI(string versionType)
        {
            HttpClientHandler httpClientHandler = new()
            {
                AllowAutoRedirect = false
            };
            using var client = new HttpClient(httpClientHandler);
            client.DefaultRequestHeaders.Add("user-agent", $"{SharedConstructs.Name}");

            var response = client.GetAsync(repoAPI);
            response.Wait();

            var result = JSON.Parse(await response.Result.Content.ReadAsStringAsync());

            for (int i = 0; i < result["assets"].Count; i++)
            {
                if (result["assets"][i]["browser_download_url"].ToString().Contains(versionType))
                {
                    Logger.Debug($"Web update resource found: {result["assets"][i]["browser_download_url"]}");
                    Uri.TryCreate(result["assets"][i]["browser_download_url"].ToString().Replace('"', ' ').Trim(), 0, out Uri resultUri);
                    return resultUri;
                }
            }
            return null;
        }
    }
}

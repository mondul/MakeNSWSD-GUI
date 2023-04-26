using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace MakeNSWSD
{
    public partial class LogWindow : Window
    {
        #pragma warning disable CS0649
        // Internal classes for JSON parsing
        [DataContract]
        internal class Asset
        {
            [DataMember(Name = "browser_download_url")]
            internal string browserDownloadURL;
        }

        [DataContract]
        internal class Release
        {
            [DataMember(Name = "tag_name")]
            internal string tagName;

            [DataMember]
            internal Asset[] assets;
        }
        #pragma warning restore CS0649

        /// <summary>
        /// Gets info on a GitHub's repo latest release and downloads its assets
        /// </summary>
        /// <param name="repo">Must be formatted as {author}/{repo}</param>
        /// <param name="filterRegex">Download only assets that matches with this filter</param>
        /// <returns>Array of the downloaded assets file paths</returns>
        private async Task<string[]> GetLatestAssets(string repo, Regex filterRegex)
        {
            Release release = new Release();

            using (HttpResponseMessage response = await _client.GetAsync($"https://api.github.com/repos/{repo}/releases/latest", _cancellationTokenSource.Token))
            {
                response.EnsureSuccessStatusCode();

                // Parse JSON
                // TODO: Do this using System.Text.Json when gets included by default in .NET
                using (MemoryStream memoryStream = new MemoryStream(await response.Content.ReadAsByteArrayAsync()))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(release.GetType());
                    release = serializer.ReadObject(memoryStream) as Release;
                }
            }

            logTxt.AppendText($"* {repo} latest release: {release.tagName}\r\n");

            List<string> downloadedFiles = new List<string>();

            foreach (Asset asset in release.assets)
            {
                // Download files that matches the filter regex
                if (filterRegex.IsMatch(asset.browserDownloadURL))
                {
                    Uri uri = new Uri(asset.browserDownloadURL);
                    string fileName = Uri.UnescapeDataString(uri.Segments.Last());
                    string filePath = Path.Combine(_workDir, fileName);

                    // Check if file exists
                    if (File.Exists(filePath))
                    {
                        logTxt.AppendText($"- {fileName} already exists\r\n");
                    }
                    else
                    {
                        logTxt.AppendText($"Downloading {fileName}... ");
                        await DownloadFile(uri, filePath);
                        logTxt.AppendText("Done\r\n");
                    }

                    downloadedFiles.Add(filePath);
                }
            }

            logTxt.AppendText("\r\n");

            return downloadedFiles.ToArray();
        }
    }
}

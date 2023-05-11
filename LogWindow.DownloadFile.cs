using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MakeNSWSD
{
    public partial class LogWindow : Window
    {
        /// <summary>
        /// Downloads a file
        /// </summary>
        /// <param name="uri">File URL</param>
        /// <param name="filePath">Output file path, including file name and extension</param>
        /// <returns></returns>
        private async Task DownloadFile(Uri uri, string filePath)
        {
            using (WebClient client = new WebClient())
            {
                using (CancellationTokenRegistration registration = _cancellationTokenSource.Token.Register(() => client.CancelAsync()))
                {
                    await client.DownloadFileTaskAsync(uri, filePath);
                }
            }
        }

        /// <summary>
        /// Downloads a file to the workdir
        /// </summary>
        /// <param name="fileUrl">File URL to download</param>
        /// <returns>Downloaded file path, including file name and extension</returns>
        private async Task<string> DownloadFile(string fileUrl)
        {
            Uri uri = new Uri(fileUrl);
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

            return filePath;
        }
    }
}

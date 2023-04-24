using System;
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
    }
}

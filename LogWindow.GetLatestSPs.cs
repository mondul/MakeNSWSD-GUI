using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MakeNSWSD
{
    public partial class LogWindow : Window
    {
        private static readonly byte[] compressedForumURL = new byte[] {
            0x04, 0xC0, 0x01, 0x0E, 0x82, 0x30, 0x0C, 0x05,
            0xD0, 0x13, 0x95, 0x6A, 0x90, 0x98, 0x78, 0x9B,
            0x8A, 0x7F, 0xAB, 0x21, 0xB8, 0x66, 0xFF, 0x7B,
            0x7F, 0x5E, 0x4A, 0xC5, 0x97, 0x7B, 0x7F, 0x87,
            0x70, 0xD6, 0xF2, 0x83, 0x5C, 0x39, 0x11, 0x1F,
            0x3A, 0xBF, 0xBD, 0x42, 0x7B, 0x82, 0xD6, 0xC6,
            0xB4, 0xD0, 0x39, 0x58, 0x89, 0x09, 0x4B, 0x1C,
            0x21, 0x58, 0x23, 0x6F, 0xD6, 0xFE, 0x04, 0xAC,
            0x62, 0x3F, 0xA2, 0x63, 0x5D, 0xB6, 0xE7, 0x7D,
            0x7B, 0xAC, 0x7E, 0x05, 0x00, 0x00, 0xFF, 0xFF,
        };

        /// <summary>
        /// Downloads the latest SPs from the forum post
        /// </summary>
        /// <returns>The downloaded .zip path</returns>
        private async Task<string> GetLatestSPs()
        {
            MemoryStream input = new MemoryStream(compressedForumURL);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }

            string forumURL = Encoding.UTF8.GetString(output.ToArray());

            string downloadURL = string.Empty;
            string fileName = string.Empty;

            using (HttpResponseMessage response = await _client.GetAsync(forumURL, _cancellationTokenSource.Token))
            {
                response.EnsureSuccessStatusCode();

                using (StringReader reader = new StringReader(await response.Content.ReadAsStringAsync()))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        int textStart = line.IndexOf("/attachments/hekate-ams");
                        if (textStart > 0)
                        {
                            int textEnd = line.IndexOf("\" target", textStart);
                            downloadURL = forumURL.Substring(0, 19) + line.Substring(textStart, textEnd - textStart);
                            continue;
                        }

                        textStart = line.IndexOf("Hekate+AMS");
                        if (textStart > 0)
                        {
                            int textEnd = line.IndexOf("\">", textStart);
                            fileName = line.Substring(textStart, textEnd - textStart);
                            break;
                        }
                    }
                }
            }

            string filePath = Path.Combine(_workDir, fileName);

            // Check if file exists
            if (File.Exists(filePath))
            {
                logTxt.AppendText($"- {fileName} already exists\r\n\r\n");
            }
            else
            {
                logTxt.AppendText($"Downloading {fileName}... ");
                await DownloadFile(new Uri(downloadURL), filePath);
                logTxt.AppendText("Done\r\n\r\n");
            }

            return filePath;
        }
    }
}

using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Windows;

namespace MakeNSWSD
{
    public partial class LogWindow : Window
    {
        private void ExtractZip(string zipFile, Regex ignoreRegex)
        {
            logTxt.AppendText($"Extracting {zipFile.Substring(8)}... ");

            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (ignoreRegex != null && ignoreRegex.IsMatch(entry.FullName))
                    {
                        continue;
                    }

                    char lastChar = entry.FullName[entry.FullName.Length - 1];
                    string extractPath = Path.Combine(_outDir, entry.FullName);

                    // Check if current entry is a directory
                    if (
                        lastChar == '/' ||
                        lastChar == '\\' ||
                        (entry.ExternalAttributes & 0x40000010) != 0
                    )
                    {
                        Directory.CreateDirectory(extractPath);
                    }
                    else
                    {
                        entry.ExtractToFile(extractPath);
                    }
                }
            }

            logTxt.AppendText("Done\r\n\r\n");
        }
    }
}

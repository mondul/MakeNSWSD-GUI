using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Windows;

namespace MakeNSWSD
{
    public partial class LogWindow : Window
    {
        private void ExtractZip(string zipFile)
        {
            ExtractZip(zipFile, _outDir, null);
        }

        private void ExtractZip(string zipFile, Regex ignoreRegex)
        {
            ExtractZip(zipFile, _outDir, ignoreRegex);
        }

        /// <summary>
        /// Method to extract a .zip file to an output directory ignoring
        /// files which names matches to a regular expression
        /// </summary>
        /// <param name="zipFile">File to extract</param>
        /// <param name="outDir">Output directory</param>
        /// <param name="ignoreRegex">Regex to match file names that will be ignored</param>
        private void ExtractZip(string zipFile, string outDir, Regex ignoreRegex)
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

                    string extractPath = Path.Combine(outDir, entry.FullName);

                    // Check if current entry is a directory
                    char lastChar = entry.FullName[entry.FullName.Length - 1];
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

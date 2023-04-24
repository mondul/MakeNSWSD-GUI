using System.IO;
using System.Windows;

namespace MakeNSWSD
{
    public partial class LogWindow : Window
    {
        /// <summary>
        /// Checks for a bootlogo.zip patch file in the workdir and
        /// extracts it to the atmosphere\exefs_patches folder
        /// </summary>
        private void ExtractBootLogo()
        {
            string bootLogoZip = Path.Combine(_workDir, "bootlogo.zip");

            if (!File.Exists(bootLogoZip))
            {
                return;
            }

            ExtractZip(bootLogoZip, Path.Combine(_outDir, "atmosphere", "exefs_patches"), null);
        }
    }
}

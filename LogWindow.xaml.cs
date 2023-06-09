using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace MakeNSWSD
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        private const string _workDir = "workdir";
        private bool _done = false;
        private static readonly HttpClient _client = new HttpClient();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly string _outDir;
        private static readonly byte[] compressedLockpickRepo = new byte[] {
            0x0A, 0xCE, 0xC9, 0x29, 0xAD, 0xA8, 0xD0, 0x0F,
            0xC8, 0x4C, 0xCE, 0xCE, 0xC9, 0x4F, 0xCE, 0x8E,
            0x0F, 0x72, 0xF6, 0x05, 0x04, 0x00, 0x00, 0xFF,
            0xFF,
        };

        private readonly bool _doAtmosphere;
        private readonly bool _doHekate;
        private readonly bool _doSPs;
        private readonly bool _doDBI;
        private readonly bool _doPayloadBin;
        private readonly bool _doBootDat;
        private readonly bool _doLockpick;

        public LogWindow(string outDir, byte checks)
        {
            // GitHub API requires an user agent to be sent
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("curl/4.0");

            _outDir = outDir;

            _doAtmosphere = (checks & 0x1) != 0;
            _doHekate = (checks & 0x2) != 0;
            _doSPs = (checks & 0x4) != 0;
            _doDBI = (checks & 0x8) != 0;
            _doPayloadBin = (checks & 0x10) != 0;
            _doBootDat = (checks & 0x20) != 0;
            _doLockpick = (checks & 0x40) != 0;

            // Open window
            InitializeComponent();
            // Attach to main window as a child
            Owner = App.Current.MainWindow;
            // Add event after the window content is rendered
            ContentRendered += LogWindow_ContentRendered;
        }

        /// <summary>
        /// Process all after the window's content has been rendered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LogWindow_ContentRendered(object sender, EventArgs e)
        {
            // Unset the event to avoid refiring
            ContentRendered -= LogWindow_ContentRendered;

            // Filenames
            string atmosphereZipFile = string.Empty;
            string hekateZipFile = string.Empty;
            string spsZipFile = string.Empty;
            string[] dbiBinFiles = {};

            string bootDatZipFile = string.Empty;
            string lockpickBinFile = string.Empty;

            try
            {
                // We'll use this folder for all downloaded files
                Directory.CreateDirectory(_workDir);

                #region DownloadFiles

                if (_doAtmosphere)
                {
                    string[] assets = await GetLatestAssets("Atmosphere-NX/Atmosphere", new Regex(@"\.zip$"));
                    atmosphereZipFile = assets.FirstOrDefault();
                }

                if (_doHekate)
                {
                    string[] assets = await GetLatestAssets("CTCaer/hekate", new Regex(@"hekate_ctcaer.+\.zip$"));
                    hekateZipFile = assets.FirstOrDefault();

                    if (_doBootDat)
                    {
                        // Do not fail if SX Gear boot files download fails
                        try
                        {
                            bootDatZipFile = await DownloadFile("https://raw.githubusercontent.com/mondul/MakeNSWSD-GUI/main/sxgearboot.zip");
                            logTxt.AppendText("\r\n");
                        }
                        catch (Exception sxEx)
                        {
                            logTxt.AppendText($"\r\n! SX Gear boot files download error: {sxEx.Message}\r\n\r\n");
                        }
                    }

                    if (_doLockpick)
                    {
                        // Do not fail if Lockpick_RCM download fails
                        try
                        {
                            MemoryStream input = new MemoryStream(compressedLockpickRepo);
                            MemoryStream output = new MemoryStream();
                            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
                            {
                                dstream.CopyTo(output);
                            }

                            assets = await GetLatestAssets(Encoding.UTF8.GetString(output.ToArray()), new Regex(@"\.bin$"));
                            lockpickBinFile = assets.FirstOrDefault();
                        }
                        catch (Exception binEx)
                        {
                            logTxt.AppendText($"\r\n! Lockpick_RCM download error: {binEx.Message}\r\n\r\n");
                        }
                    }
                }

                if (_doSPs)
                {
                    // Do not fail if SPs download fails
                    try
                    {
                        spsZipFile = await GetLatestSPs();
                    }
                    catch (Exception zipEx)
                    {
                        logTxt.AppendText($"\r\n! SPs download error: {zipEx.Message}\r\n\r\n");
                    }
                }

                if (_doDBI)
                {
                    // Do not fail if DBI download fails
                    try
                    {
                        dbiBinFiles = await GetLatestAssets("rashevskyv/dbi", new Regex(@"((dbi\.config)|(DBI\.nro))$"));
                    }
                    catch (Exception binEx)
                    {
                        logTxt.AppendText($"\r\n! DBI download error: {binEx.Message}\r\n\r\n");
                    }
                }

                #endregion

                logTxt.AppendText("-------\r\n\r\n");

                #region ExtractAndCopyFiles

                if (_doAtmosphere)
                {
                    ExtractZip(atmosphereZipFile);

                    // Do not fail if ban prevention files creation fails
                    try
                    {
                        BanPrevention();
                    }
                    catch (Exception banEx)
                    {
                        logTxt.AppendText($"\r\n! Ban prevention files creation error: {banEx.Message}\r\n\r\n");
                    }

                    // Do not fail if boot logo extraction fails
                    try
                    {
                        ExtractBootLogo();
                    }
                    catch (Exception zipEx)
                    {
                        logTxt.AppendText($"\r\n! Boot logo extraction error: {zipEx.Message}\r\n\r\n");
                    }
                }

                if (_doHekate)
                {
                    ExtractZip(hekateZipFile, _doPayloadBin ? null : new Regex(@"^hekate_ctcaer_.+\.bin$"));

                    if (_doPayloadBin)
                    {
                        logTxt.AppendText("Renaming Hekate payload... ");
                        // Do not fail if renaming Hekate payload fails
                        try
                        {
                            File.Move(
                                Directory.GetFiles(_outDir, "hekate_ctcaer_*.bin", SearchOption.TopDirectoryOnly)[0],
                                Path.Combine(_outDir, "payload.bin")
                            );
                            logTxt.AppendText("Done\r\n\r\n");
                        }
                        catch (Exception renEx)
                        {
                            logTxt.AppendText($"\r\n! payload.bin renaming error: {renEx.Message}\r\n\r\n");
                        }
                    }
                    else if (_doBootDat)
                    {
                        // Do not fail if SX Gear boot files extraction fails
                        try
                        {
                            ExtractZip(bootDatZipFile);
                        }
                        catch (Exception zipEx)
                        {
                            logTxt.AppendText($"\r\n! SX Gear boot files extraction error: {zipEx.Message}\r\n\r\n");
                        }
                    }

                    if (_doLockpick && lockpickBinFile.Length > 0)
                    {
                        logTxt.AppendText("Moving Lockpick_RCM to payloads... ");
                        // Do not fail if moving Lockpick_RCM.bin fails
                        try
                        {
                            string destFile = Path.Combine(_outDir, "bootloader", "payloads", lockpickBinFile.Substring(8));
                            File.Move(lockpickBinFile, destFile);

                            logTxt.AppendText("Done\r\n\r\n");
                        }
                        catch (Exception binEx)
                        {
                            logTxt.AppendText($"\r\n! Lockpick move error: {binEx.Message}\r\n\r\n");
                        }
                    }
                }

                if (_doSPs && spsZipFile.Length > 0)
                {
                    // Do not fail if SPs extraction fails
                    try
                    {
                        ExtractZip(spsZipFile);
                    }
                    catch (Exception zipEx)
                    {
                        logTxt.AppendText($"\r\n! SPs extraction error: {zipEx.Message}\r\n\r\n");
                    }
                }

                if (_doDBI && dbiBinFiles.Length > 0)
                {
                    logTxt.AppendText("Moving DBI files... ");
                    // Do not fail if moving DBI files fails
                    try
                    {
                        string dbiFolder = Path.Combine(_outDir, "switch", "DBI");
                        Directory.CreateDirectory(dbiFolder);

                        foreach (string srcFile in dbiBinFiles)
                        {
                            string destFile = Path.Combine(dbiFolder, srcFile.Substring(8));
                            File.Move(srcFile, destFile);
                        }

                        logTxt.AppendText("Done\r\n\r\n");
                    }
                    catch (Exception binEx)
                    {
                        logTxt.AppendText($"\r\n! DBI move error: {binEx.Message}\r\n\r\n");
                    }
                }

                #endregion

                statusTxt.Text = "Done";

            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Helper process interrupted", "Warning - Make NSW SD", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                logTxt.AppendText($"\r\n--------\r\n! Unexpected error: {ex.Message}");
                statusTxt.Text = "Unexpected error!";
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _done = true;
            }
        }

        /// <summary>
        /// Warn on close if the helper process is running
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_done)
            {
                Environment.Exit(0);
                return;
            }

            if (MessageBox.Show("Are you sure you want to interrupt the process?", "Interrupt - Make NSW SD",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }

            _cancellationTokenSource?.Cancel();
        }
    }
}

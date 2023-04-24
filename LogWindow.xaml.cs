using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
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

        private readonly bool _doAtmosphere;
        private readonly bool _doHekate;
        private readonly bool _doSPs;
        private readonly bool _doLockpick;
        private readonly bool _doDBI;

        public LogWindow(string outDir, byte checks)
        {
            // GitHub API requires an user agent to be sent
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("curl/4.0");

            _outDir = outDir;

            _doAtmosphere = (checks & 0x10) != 0;
            _doHekate = (checks & 0x8) != 0;
            _doSPs = (checks & 0x4) != 0;
            _doLockpick = (checks & 0x2) != 0;
            _doDBI = (checks & 0x1) != 0;

            // Open window
            InitializeComponent();
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
            string lockpickBinFile = string.Empty;
            string[] dbiBinFiles = {};

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

                if (_doLockpick)
                {
                    // Do not fail if Lockpick_RCM download fails
                    try
                    {
                        string[] assets = await GetLatestAssets("shchmue/Lockpick_RCM", new Regex(@"\.bin$"));
                        lockpickBinFile = assets.FirstOrDefault();
                    }
                    catch (Exception binEx)
                    {
                        logTxt.AppendText($"\r\n! Lockpick_RCM download error: {binEx.Message}\r\n\r\n");
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
                }

                if (_doHekate)
                {
                    ExtractZip(hekateZipFile, new Regex(@"^hekate_ctcaer_.+\.bin$"));
                }

                if (_doSPs)
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

                if (_doLockpick)
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

                if (_doDBI)
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
                Owner.Close();
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

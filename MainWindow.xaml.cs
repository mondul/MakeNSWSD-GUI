using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MakeNSWSD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region ForAbout

        // Win32 API methods for gaining access to the WPF Window’s system ContextMenu and inserting our custom item into it
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool InsertMenu(IntPtr hMenu, int wPosition, int wFlags, int wIDNewItem, string lpNewItem);

        //A window receives this message when the user chooses a command from the Window menu, or when the user chooses the maximize button, minimize button, restore button, or close button.
        private const int WM_SYSCOMMAND = 0x112;

        //Draws a horizontal dividing line.This flag is used only in a drop-down menu, submenu, or shortcut menu.The line cannot be grayed, disabled, or highlighted.
        private const int MF_SEPARATOR = 0x800;

        //Specifies that an ID is a position index into the menu and not a command ID.
        private const int MF_BYPOSITION = 0x400;

        //Menu Id for our custom menu item.
        private const int _aboutMenuItemId = 1000;

        #endregion

        #region ForChecks

        private byte _checks = 0x7;

        private void EnableStartBtn()
        {
            startBtn.IsEnabled = (_checks & 0xF) != 0;
        }

        public bool AtmosphereCheck
        {
            get { return (_checks & 0x1) != 0; }
            set
            {
                _checks = (byte)(value ? (_checks | 0x1) : (_checks & 0x7E));
                EnableStartBtn();
            }
        }

        public bool HekateCheck
        {
            get { return (_checks & 0x2) != 0; }
            set
            {
                _checks = (byte)(value ? (_checks | 0x2) : (_checks & 0x7D));
                payloadBinChk.IsEnabled = value;
                bootDatChk.IsEnabled = value;
                lockpickChk.IsEnabled = value;
                EnableStartBtn();
            }
        }

        public bool SPsCheck
        {
            get { return (_checks & 0x4) != 0; }
            set
            {
                _checks = (byte)(value ? (_checks | 0x4) : (_checks & 0x7B));
                EnableStartBtn();
            }
        }

        public bool DBICheck
        {
            get { return (_checks & 0x8) != 0; }
            set
            {
                _checks = (byte)(value ? (_checks | 0x8) : (_checks & 0x77));
                EnableStartBtn();
            }
        }

        public bool PayloadBinCheck
        {
            get { return (_checks & 0x10) != 0; }
            set
            {
                if (value)
                {
                    _checks = (byte)(_checks | 0x10);
                    bootDatChk.IsChecked = false; // Triggers BootDatCheck's else
                }
                else
                {
                    _checks = (byte)(_checks & 0x6F);
                }
            }
        }

        public bool BootDatCheck
        {
            get { return (_checks & 0x20) != 0; }
            set
            {
                if (value)
                {
                    _checks = (byte)(_checks | 0x20);
                    payloadBinChk.IsChecked = false; // Triggers PayloadBinCheck's else
                }
                else
                {
                    _checks = (byte)(_checks & 0x5F);
                }
            }
        }

        public bool LockpickCheck
        {
            get { return (_checks & 0x40) != 0; }
            set { _checks = (byte)(value ? (_checks | 0x40) : (_checks & 0x3F)); }
        }

        #endregion

        private readonly string _defaultFolder;

        public MainWindow()
        {
            // Initialize vars
            DateTimeOffset now = (DateTimeOffset)DateTime.UtcNow;
            _defaultFolder = $"SD_{now.ToUnixTimeSeconds():X}";
            // Open window
            InitializeComponent();
            // Set initial texts
            outFolderTxt.Text = _defaultFolder;
            // Add event listener
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Insert the "About..." menu item in the context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Unset the event to avoid refiring
            Loaded -= MainWindow_Loaded;

            IntPtr windowhandle = new WindowInteropHelper(this).Handle;
            HwndSource hwndSource = HwndSource.FromHwnd(windowhandle);

            //Get the handle for the system menu
            IntPtr systemMenuHandle = GetSystemMenu(windowhandle, false);

            //Insert our custom menu item after Maximize
            InsertMenu(systemMenuHandle, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty); //Add a menu separator
            InsertMenu(systemMenuHandle, 6, MF_BYPOSITION, _aboutMenuItemId, "About Make NSW SD"); //Add the about menu item

            hwndSource.AddHook(new HwndSourceHook(WndProc));
        }

        /// <summary>
        /// Method added to the window's event loop, checks if the About menu
        /// entry was clicked
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns>0</returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Check if the SystemCommand message has been executed
            if (msg == WM_SYSCOMMAND)
            {
                //check which menu item was clicked
                switch (wParam.ToInt32())
                {
                    case _aboutMenuItemId:
                        MessageBox.Show($"Make NSW SD\r\nVersion {Assembly.GetExecutingAssembly().GetName().Version}\r\n\r\nhttps://github.com/mondul",
                            "About - Make NSW SD", MessageBoxButton.OK, MessageBoxImage.Information);
                        handled = true;
                        break;
                }
            }

            return IntPtr.Zero;
        }

        private void BrowseOutFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Please choose your SD card drive path or an output folder for the extracted files:"
            };

            outFolderTxt.Text = folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? folderDialog.SelectedPath : _defaultFolder;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            LogWindow logWindow = new LogWindow(outFolderTxt.Text, _checks);
            logWindow.ShowDialog();
        }

        private void QuitBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

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

        private byte _checks = 0x1C;

        private void EnableStartBtn()
        {
            startBtn.IsEnabled = _checks != 0;
        }

        public bool AtmosphereCheck
        {
            get { return (_checks & 0x10) != 0; }
            set {
                _checks = (byte)(value ? (_checks | 0x10) : (_checks & 0xF));
                EnableStartBtn();
            }
        }

        public bool HekateCheck
        {
            get { return (_checks & 0x8) != 0; }
            set {
                _checks = (byte)(value ? (_checks | 0x8) : (_checks & 0x17));
                EnableStartBtn();
            }
        }

        public bool SPsCheck
        {
            get { return (_checks & 0x4) != 0; }
            set {
                _checks = (byte)(value ? (_checks | 0x4) : (_checks & 0x1B));
                EnableStartBtn();
            }
        }

        public bool LockpickCheck
        {
            get { return (_checks & 0x2) != 0; }
            set {
                _checks = (byte)(value ? (_checks | 0x2) : (_checks & 0x1D));
                EnableStartBtn();
            }
        }

        public bool DBICheck
        {
            get { return (_checks & 0x1) != 0; }
            set {
                _checks = (byte)(value ? (_checks | 0x1) : (_checks & 0x1E));
                EnableStartBtn();
            }
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

            EnableStartBtn();
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

// File: HelpCard/MainWindow.xaml.cs
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Runtime.InteropServices;          // Win32 clipboard + focus
using System.Threading.Tasks;                 // Task.Delay
using System.Windows.Interop;                 // WindowInteropHelper

namespace HelpCard
{
    public partial class MainWindow : Window
    {
        private string logonTime;

        public MainWindow()
        {
            InitializeComponent();
            LoadSystemInfo();
        }

        private void LoadSystemInfo()
        {
            ComputerNameText.Text = Environment.MachineName;

            var ipAddresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                             !ip.Address.ToString().StartsWith("127."))
                .Select(ip => ip.Address.ToString())
                .Distinct()
                .ToList();
            IpList.ItemsSource = ipAddresses;

            CurrentUserText.Text = Environment.UserName;
            logonTime = GetLogonTime();
            DefaultPrinterText.Text = GetDefaultPrinter();
            OsText.Text = GetOsFriendlyName();
            RebootTimeText.Text = GetLastRebootTime();
        }

        private string GetDefaultPrinter()
        {
            try
            {
                var printServer = new LocalPrintServer();
                var defaultQueue = printServer.DefaultPrintQueue;
                return defaultQueue.FullName;
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetOsFriendlyName()
        {
            var version = Environment.OSVersion.Version;
            string osName = version.Major switch
            {
                10 when version.Build >= 22000 => "Windows 11",
                10 => "Windows 10",
                _ => "Windows"
            };
            return $"{osName} (Build {version.Build})";
        }

        private string GetLastRebootTime()
        {
            try
            {
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                var lastBoot = DateTime.Now - uptime;
                return lastBoot.ToString("g");
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetLogonTime()
        {
            try
            {
                return Process.GetProcessesByName("explorer")
                    .OrderBy(p => p.StartTime)
                    .FirstOrDefault()?.StartTime.ToString("g") ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetSystemInfoString()
        {
            return $"Computer Name: {ComputerNameText.Text}\n" +
                   $"User: {CurrentUserText.Text}\n" +
                   $"Logon Time: {logonTime}\n" +
                   $"Printer: {DefaultPrinterText.Text}\n" +
                   $"OS: {OsText.Text}\n" +
                   $"Last Reboot: {RebootTimeText.Text}\n" +
                   $"IP(s): {string.Join(", ", IpList.Items.Cast<string>())}";
        }

        // --- COPY: Native clipboard path (robust under focus/contention) ---
        private async void Copy_Click(object sender, RoutedEventArgs e)
        {
            string info = GetSystemInfoString();

            // Gentle foreground nudge helps OpenClipboard succeed more often.
            BringWindowToForeground();
            await Task.Delay(60);

            var hwnd = new WindowInteropHelper(this).Handle;

            const int maxRetries = 10;
            int delayMs = 80;

            bool ok = false;
            for (int i = 0; i < maxRetries; i++)
            {
                if (TrySetClipboardUnicodeNative(hwnd, info))
                {
                    ok = true;
                    break;
                }

                await Task.Delay(delayMs);
                if (delayMs < 800) delayMs *= 2; // backoff
            }

            if (ok)
            {
                ShowTemporaryButtonFeedback(CopyButton, "Copied!");
            }
            else
            {
                MessageBox.Show(
                    "Unable to copy info to clipboard after several attempts.",
                    "Clipboard Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Native clipboard: CF_UNICODETEXT via Win32, bypassing OLE/WPF Clipboard.
        private static bool TrySetClipboardUnicodeNative(nint ownerHwnd, string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // OpenClipboard can fail if another app holds it; we only attempt once here.
            if (!OpenClipboard(ownerHwnd)) return false;

            try
            {
                if (!EmptyClipboard())
                    return false;

                // Allocate global memory for UTF-16 text + null terminator
                int bytes = (text.Length + 1) * 2;
                nint hGlobal = GlobalAlloc(GHND, (nuint)bytes);
                if (hGlobal == 0) return false;

                nint ptr = 0;
                try
                {
                    ptr = GlobalLock(hGlobal);
                    if (ptr == 0) return false;

                    // Copy string into unmanaged buffer
                    Marshal.Copy(text.ToCharArray(), 0, ptr, text.Length);
                    // Null-terminate
                    Marshal.WriteInt16(ptr, text.Length * 2, 0);

                    // After SetClipboardData, clipboard owns the memory; do NOT free it.
                    if (SetClipboardData(CF_UNICODETEXT, hGlobal) == 0)
                        return false;

                    hGlobal = 0; // ownership transferred
                    return true;
                }
                finally
                {
                    if (ptr != 0) GlobalUnlock(hGlobal);
                    if (hGlobal != 0) GlobalFree(hGlobal);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
        private void EpicHelp_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://access.providence.org/";

            string[] chromePaths =
            {
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
    };

            try
            {
                // Try each Chrome path
                foreach (var path in chromePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = url,
                            UseShellExecute = true
                        });
                        return; // success
                    }
                }

                // Fallback to default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open Epic help page: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        // --- Email ---

        private void EmailIT_Click(object sender, RoutedEventArgs e)
        {
            string subject = Uri.EscapeDataString("I.T. Help Request");
            string body = Uri.EscapeDataString("\n\n" + GetSystemInfoString());

            string mailto = $"mailto:it@pullmanregionalhospital.freshservice.com?subject={subject}&body={body}";

            try
            {
                Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
            }
            catch
            {
                // Fallback: attempt native clipboard too
                _ = TrySetClipboardUnicodeNative(new WindowInteropHelper(this).Handle, body);
                MessageBox.Show("Unable to open email client. Info copied to clipboard instead.",
                                "Email Error", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ShowTemporaryButtonFeedback(Button button, string message)
        {
            string originalContent = button.Content.ToString();
            button.Content = message;
            button.IsEnabled = false;

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, e) =>
            {
                button.Content = originalContent;
                button.IsEnabled = true;
                timer.Stop();
            };
            timer.Start();
        }

        // --- Foreground helpers ---

        private void BringWindowToForeground()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;

                Topmost = true;
                Topmost = false;

                ShowWindow(hwnd, SW_RESTORE);
                BringWindowToTop(hwnd);
                SetForegroundWindow(hwnd);

                Activate();
                Focus();
            }
            catch
            {
                // best effort
            }
        }

        // --- Win32 interop ---

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;
        private const uint GMEM_ZEROINIT = 0x0040;
        private const uint GHND = GMEM_MOVEABLE | GMEM_ZEROINIT;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(nint hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint SetClipboardData(uint uFormat, nint hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GlobalAlloc(uint uFlags, nuint dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GlobalLock(nint hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(nint hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GlobalFree(nint hMem);

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hWnd);
        [DllImport("user32.dll")] private static extern bool BringWindowToTop(nint hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(nint hWnd, int nCmdShow);
    }
}

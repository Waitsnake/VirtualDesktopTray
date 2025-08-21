using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VirtualDesktopTray
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DestroyIcon(IntPtr hIcon);

        delegate int GetCurrentDesktopNumberDelegate();
        delegate int GetDesktopCountDelegate();		
		delegate int GoToDesktopNumberDelegate(int desktopNumber);
		
		static GetCurrentDesktopNumberDelegate? GetCurrentDesktopNumber;
		static GoToDesktopNumberDelegate? GoToDesktopNumber;		
        static GetDesktopCountDelegate? GetDesktopCount;


        private static NotifyIcon? trayIcon;
        private static System.Windows.Forms.Timer? updateTimer;
        private static Icon? currentIcon;

        [STAThread]
        static void Main()
        {
			Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
			
			// Check Windows Version
            Version osVersion = Environment.OSVersion.Version;
            if (osVersion.Major < 10 || (osVersion.Major == 10 && osVersion.Build < 22000))
            {
                MessageBox.Show(
                    "This Software needs Windows 11 or higher.",
                    "VirtualDesktopTray - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }
			
			
			// DLL laden
            string vdaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VirtualDesktopAccessor.dll");
            if (!File.Exists(vdaPath))
            {
                MessageBox.Show(
                    "VirtualDesktopAccessor.dll was not found.\n\n" +
                    "Please put this DLL in same Directrory as VirtualDesktopTray.exe.\n\n" +
                    "Download:\nhttps://github.com/Ciantic/VirtualDesktopAccessor/releases",
                    "VirtualDesktopTray - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }
        
            IntPtr hVDA = LoadLibrary(vdaPath);
            if (hVDA == IntPtr.Zero)
            {
                MessageBox.Show(
                    "VirtualDesktopAccessor.dll could not load.\n" +
                    "Maybe wrong Version (e.g. Windows 10 Binary and not Windows 11).",
                    "VirtualDesktopTray - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            // Wichtige Funktionen prüfen
            IntPtr pGetCurrentDesktop = GetProcAddress(hVDA, "GetCurrentDesktopNumber");
            IntPtr pGetDesktopCount  = GetProcAddress(hVDA, "GetDesktopCount");
            IntPtr pGoToDesktop      = GetProcAddress(hVDA, "GoToDesktopNumber");
            
            if (pGetCurrentDesktop == IntPtr.Zero || pGetDesktopCount == IntPtr.Zero || pGoToDesktop == IntPtr.Zero)
            {
                MessageBox.Show(
                    "The loaded VirtualDesktopAccessor.dll is not compatible.\n\n" +
                    "Please load a new Windows 11 Version from:\n" +
                    "https://github.com/Ciantic/VirtualDesktopAccessor/releases",
                    "VirtualDesktopTray - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }
            
            // Delegates setzen
            GetCurrentDesktopNumber = Marshal.GetDelegateForFunctionPointer<GetCurrentDesktopNumberDelegate>(pGetCurrentDesktop);
            GetDesktopCount         = Marshal.GetDelegateForFunctionPointer<GetDesktopCountDelegate>(pGetDesktopCount);
            GoToDesktopNumber       = Marshal.GetDelegateForFunctionPointer<GoToDesktopNumberDelegate>(pGoToDesktop);			

            trayIcon = new NotifyIcon();
            trayIcon.Visible = true;
            currentIcon = CreateTextIcon("0");
            trayIcon.Icon = currentIcon;
            trayIcon.Text = "Current Virtual Desktop";
			trayIcon.MouseClick += TrayIcon_MouseClick;

            BuildContextMenu();

            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 200;
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            var dummyForm = new Form() { ShowInTaskbar = false, WindowState = FormWindowState.Minimized };
            dummyForm.Load += (s, e) => dummyForm.Hide();

            Application.Run(dummyForm);
        }
		
		static void BuildContextMenu()
        {
            // Altes Menü entsorgen, wenn vorhanden
            if (trayIcon!.ContextMenuStrip != null)
            {
                trayIcon.ContextMenuStrip.Dispose();
            }
        
            var menu = new ContextMenuStrip();
        
            int desktopCount = GetDesktopCount != null ? GetDesktopCount() : 1;
            for (int i = 0; i < desktopCount; i++)
            {
                int desktopNumber = i;
                var item = new ToolStripMenuItem($"Desktop {i + 1}");
                item.Click += (s, e) => GoToDesktopNumber?.Invoke(desktopNumber);
                menu.Items.Add(item);
            }
        
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += Exit;
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);
        
            trayIcon.ContextMenuStrip = menu;
        }

        private static int lastDesktopCount = 0;

        private static void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (GetCurrentDesktopNumber != null && trayIcon != null)
                {
                    int current = GetCurrentDesktopNumber() + 1;
        
                    if (currentIcon != null)
                    {
                        DestroyIcon(currentIcon.Handle);
                        currentIcon.Dispose();
                    }
        
                    currentIcon = CreateTextIcon(current.ToString());
                    trayIcon.Icon = currentIcon;
                    trayIcon.Text = $"Virtual Desktop {current}";
                }
        
                // Prüfen ob sich die Desktopanzahl geändert hat
                if (GetDesktopCount != null)
                {
                    int count = GetDesktopCount();
                    if (count != lastDesktopCount)
                    {
                        BuildContextMenu();
                        lastDesktopCount = count;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Timer-Fehler: {ex.Message}");
            }
        }

        private static Icon CreateTextIcon(string text)
        {
            int size = 32;
            using Bitmap bmp = new Bitmap(size, size);
            using Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);

            // Taskbar-Farbe aus Registry lesen (hell oder dunkel)
            bool isTaskbarDark = true;
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    object? val = key.GetValue("SystemUsesLightTheme");
                    if (val is int lightTheme)
                        isTaskbarDark = lightTheme == 0;
                }
            }
            catch { }

            Color fontColor = isTaskbarDark ? Color.White : Color.Black;

            // Fontgröße anpassen für ≥2-stellige Desktops
            float fontSize = text.Length >= 2 ? 16f : 32f;

            using Font font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            using Brush brush = new SolidBrush(fontColor);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.DrawString(text, font, brush, new RectangleF(0, 0, size, size), sf);

            IntPtr hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }
		
		private static void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && 
                GetCurrentDesktopNumber != null && 
                GetDesktopCount != null && 
                GoToDesktopNumber != null)
            {
                int current = GetCurrentDesktopNumber();
                int total = GetDesktopCount();
                int next = (current + 1) % total; // zyklisch
                GoToDesktopNumber(next);
            }
        }

        private static void Exit(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "You really like to exit VirtualDesktopTray?",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
        
            if (result != DialogResult.Yes)
                return;
        
            updateTimer?.Stop();
            updateTimer?.Dispose();
        
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
        
            if (currentIcon != null)
            {
                DestroyIcon(currentIcon.Handle);
                currentIcon.Dispose();
            }
        
            foreach (Form form in Application.OpenForms)
            {
                form.Close();
            }
        
            Application.ExitThread();
        }
    }
}

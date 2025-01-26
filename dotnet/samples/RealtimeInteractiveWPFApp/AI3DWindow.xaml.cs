using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RealtimeInteractiveWPFApp
{
    /// <summary>
    /// AI3DWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AI3DWindow : Window
    {
        [DllImport("UnityPlayer.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int UnityMain(IntPtr hInstance, IntPtr hPrevInstance, [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, int nShowCmd);

        [DllImport("Ai.Unity.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetMessage(string message);
        //public static extern void SetMessage([MarshalAs(UnmanagedType.LPStr)] string message);

        // Import the SetDllDirectory function
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private extern static IntPtr FindWindowEx([Optional] IntPtr hWndParent, [Optional] IntPtr hWndChildAfter, [Optional] string? lpszClass, [Optional] string? lpszWindow);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos",
        ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        private extern static bool SetWindowPos(IntPtr hWnd, [Optional] IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SWP uFlags);

        private const int WM_SIZING = 0x214;
        private const int WM_EXITSIZEMOVE = 0x232;
        private static bool WindowWasResized = false;
        private IntPtr unityHWND = IntPtr.Zero;

        public AI3DWindow()
        {
            IntPtr hWnd = new WindowInteropHelper(this).EnsureHandle();
            int[] colorstr = new int[] { 0x111111 };
            MainWindow.DwmSetWindowAttribute(hWnd, MainWindow.DWWMA_CAPTION_COLOR, colorstr, 4);

            InitializeComponent();

            this.Loaded += AI3DWindow_Loaded;
        }

        private void AI3DWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            //source.AddHook(new HwndSourceHook(WndProc));

            LoadUnityPlayer();
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SIZING)
            {

                if (WindowWasResized == false)
                {

                    //    'indicate the the user is resizing and not moving the window
                    WindowWasResized = true;
                }
            }

            if (msg == WM_EXITSIZEMOVE)
            {

                // 'check that this is the end of resize and not move operation          
                if (WindowWasResized == true)
                {

                    // your stuff to do 
                    Console.WriteLine("End");
                    //ResizeUnityWindow();

                    // 'set it back to false for the next resize/move
                    WindowWasResized = false;
                }
            }

            return IntPtr.Zero;
        }


        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }


        private void LoadUnityPlayer()
        {
            ////// Set the directory where the DLL is located
            ////string unityPlayerPath = AppDomain.CurrentDomain.BaseDirectory + @"\UnityDLL";
            ////if (!SetDllDirectory(unityPlayerPath))
            ////{
            ////    Console.WriteLine("Failed to set DLL directory.");
            ////    return;
            ////}
            ///

            string cmd = $"-parentHWND {unityPanel.Handle.ToInt64()} -logFile log.txt";

            Dispatcher.BeginInvoke(new Action(() =>
            {
                UnityMain(Process.GetCurrentProcess().Handle, IntPtr.Zero, cmd, 1);
            }), DispatcherPriority.ContextIdle);
        }

        private void ResizeUnityWindow()
        {
            if (unityHWND == IntPtr.Zero)
            {
                unityHWND = FindWindowEx(unityPanel.Handle, IntPtr.Zero, null, null);
            }
            else
            {
                // Resize Unity window to match the Panel inside WindowsFormsHost
                var rect = unityPanel.ClientRectangle;
                //MoveWindow(unityHWND, 0, 0, rect.Width, rect.Height, true);
                SetWindowPos(unityHWND, IntPtr.Zero, 0, 0, rect.Width, rect.Height, SWP.DEFERERASE | SWP.NOACTIVATE | SWP.NOCOPYBITS | SWP.NOMOVE | SWP.NOOWNERZORDER | SWP.NOREDRAW | SWP.NOZORDER);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeUnityWindow();
        }
        [Flags]
        private enum SWP : uint
        {
            ASYNCWINDOWPOS = 0x4000,
            DEFERERASE = 0x2000,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            HIDEWINDOW = 0x0080,
            NOACTIVATE = 0x0010,
            NOCOPYBITS = 0x0100,
            NOMOVE = 0x0002,
            NOOWNERZORDER = 0x0200,
            NOREDRAW = 0x0008,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            NOSIZE = 0x0001,
            NOZORDER = 0x0004,
            SHOWWINDOW = 0x0040
        }
    }
}

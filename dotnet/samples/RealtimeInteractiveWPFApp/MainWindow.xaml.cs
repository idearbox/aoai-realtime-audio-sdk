using OpenSG.AI;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RealtimeInteractiveWPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("UnityPlayer.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int UnityMain(IntPtr hInstance, IntPtr hPrevInstance, string lpCmdLine, int nShowCmd);
        //public static extern int UnityMain(IntPtr hInstance, IntPtr hPrevInstance, [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, int nShowCmd);
        
        [DllImport("Ai.Unity.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetMessage(string message);
        //public static extern void SetMessage([MarshalAs(UnmanagedType.LPStr)] string message);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
            Application.Current.Shutdown();
        }

        private void btnInit_Click(object sender, RoutedEventArgs e)
        {
            OpenSGManager agent = new OpenSGManager();
            _=Task.Run(() => agent.RunAIAgent());
        }

        private void btnLoad3D_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("111");
            string cmd = $"-parentHWND {wfUnityHost.Handle} -logFile log.txt";
            //UnityMain(hInstFromHwnd, IntPtr.Zero, cmd, 1);
            UnityMain(IntPtr.Zero, IntPtr.Zero, cmd, 1);
        }

        private void btnLog_Click(object sender, RoutedEventArgs e)
        {
            SetMessage("hello from WPF->unity");
            Console.WriteLine("222");
        }
    }
}
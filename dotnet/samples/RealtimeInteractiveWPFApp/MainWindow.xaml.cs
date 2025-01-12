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
using System.Windows.Threading;
using Telerik.Windows.Controls.ConversationalUI;

namespace RealtimeInteractiveWPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("UnityPlayer.dll", CallingConvention = CallingConvention.Cdecl)]
        //private static extern int UnityMain(IntPtr hInstance, IntPtr hPrevInstance, string lpCmdLine, int nShowCmd);
        public static extern int UnityMain(IntPtr hInstance, IntPtr hPrevInstance, [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, int nShowCmd);

        [DllImport("Ai.Unity.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetMessage(string message);
        //public static extern void SetMessage([MarshalAs(UnmanagedType.LPStr)] string message);

        private Author currentAuthor;
        private Author aiAuthor;
        private OpenSGManager aiAgent;

        public MainWindow()
        {
            InitializeComponent();
            currentAuthor = new Author("User");
            aiAuthor = new Author("FMS AI");
            this.tkChat.CurrentAuthor = currentAuthor;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
            Application.Current.Shutdown();
        }

        private void btnInit_Click(object sender, RoutedEventArgs e)
        {
            aiAgent = new OpenSGManager();
            _ = Task.Run(() => aiAgent.RunAIAgent());
            aiAgent.OnUserMessageReceived += AiAgent_OnUserMessageReceived;
            aiAgent.OnAIMessageReceived += AiAgent_OnAiMessageReceived;
        }

        private async void AiAgent_OnUserMessageReceived(object? sender, string e)
        {
            _ = Dispatcher.InvokeAsync(new Action(() =>
            {
                if (tkChat.LastMessage == null || tkChat.LastMessage.Message.Author != currentAuthor)
                {
                    tkChat.AddMessage(currentAuthor, e);
                }
                else
                {
                    TextMessage? tm = tkChat.LastMessage?.Message as TextMessage;
                    if (tm != null && tm.Author == currentAuthor)
                    {
                        tm.Text += e;
                    }
                }
            }), DispatcherPriority.Normal);
        }

        private async void AiAgent_OnAiMessageReceived(object? sender, string e)
        {
            _ = Dispatcher.InvokeAsync(new Action(() =>
             {
                 if (tkChat.LastMessage == null || tkChat.LastMessage.Message.Author != aiAuthor)
                 {
                     tkChat.AddMessage(aiAuthor, e);
                 }
                 else
                 {
                     TextMessage? tm = tkChat.LastMessage?.Message as TextMessage;
                     if (tm != null && tm.Author == aiAuthor)
                     {
                         tm.Text += e;
                     }
                 }
             }), DispatcherPriority.Normal);
        }

        private void btnLoad3D_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("111");
            //string cmd = $"-parentHWND {wfUnityHost.Handle} -logFile log.txt";
            //string cmd = $"-parentHWND {wfUnityHost.Handle} -logFile log2.txt";
            //UnityMain(Process.GetCurrentProcess().Handle, IntPtr.Zero, cmd, 5);
            //UnityMain(IntPtr.Zero, IntPtr.Zero, cmd, 1);
            //await this.StartUnityAsync(cmd);

        }
        private void btnLog_Click(object sender, RoutedEventArgs e)
        {
            SetMessage("hello from WPF->unity");
            Console.WriteLine("222");
        }
        private async Task StartUnityAsync(string args)
        {
            Console.WriteLine("111...StartUnityAsync");
            await Task.Yield();

            try
            {
                _ = UnityMain(Process.GetCurrentProcess().Handle, IntPtr.Zero, args, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            Console.WriteLine("222...StartUnityAsync");
        }

        private void radToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (aiAgent == null || aiAgent.Mic == null)
                return;
            aiAgent.StartRecording();

            radToggleButton.Background = Brushes.Green;
        }

        private void radToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (aiAgent == null || aiAgent.Mic == null)
                return;
            aiAgent.StopRecording();
            radToggleButton.Background = Brushes.Gray;
        }


    }
}
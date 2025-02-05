using CSAudioVisualization;
using OpenAI.RealtimeConversation;
using OpenSG.AI;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.ConversationalUI;
using Telerik.Windows.Documents.Spreadsheet.Expressions.Functions;

#pragma warning disable OPENAI002
#pragma warning disable AOAI001 

namespace RealtimeInteractiveWPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("DwmApi")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);
        public const int DWWMA_CAPTION_COLOR = 35;

        private Author currentAuthor;
        private Author aiAuthor;
        private OpenSGManager aiAgent;
        private AudioVisualization audioVisualizationAI;
        private AudioVisualization audioVisualizationUser;

        private AI3DWindow ai3DWindow = new AI3DWindow();
        public MainWindow()
        {
            IntPtr hWnd = new WindowInteropHelper(this).EnsureHandle();
            int[] colorstr = new int[] { 0x111111 };
            DwmSetWindowAttribute(hWnd, DWWMA_CAPTION_COLOR, colorstr, 4);

            StyleManager.ApplicationTheme = new GreenTheme();

            InitializeComponent();
            currentAuthor = new Author("User");
            aiAuthor = new Author("FMS AI");
            this.tkChat.CurrentAuthor = currentAuthor;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            audioVisualizationAI?.Stop();
            audioVisualizationUser?.Stop();
            audioVisualizationAI?.Dispose();
            audioVisualizationUser?.Dispose();
            Close();
            System.Windows.Application.Current.Shutdown();
        }

        private void btnInit_Click(object sender, RoutedEventArgs e)
        {
            audioVisualizationAI = InitializeAudioVisualAI();
            audioVisualizationUser = InitializeAudioVisualUser();

            aiAgent = new OpenSGManager();
            _ = Task.Run(() => aiAgent.RunAIAgent());
            aiAgent.OnUserMessageReceived += AiAgent_OnUserMessageReceived;
            aiAgent.OnAIMessageReceived += AiAgent_OnAiMessageReceived;

            aiAgent.OnAIToolExecuted += AiAgent_OnAIToolExecuted;
            aiAgent.OnUserSpeechFinished += AiAgent_OnUserSpeechFinished;
        }

        private void AiAgent_OnAIToolExecuted(object? sender, string e)
        {
            _ = Dispatcher.InvokeAsync(new Action(() =>
            {
                tkChat.TypingIndicatorText = $"지식함수 실행중({e})...";
                tkChat.TypingIndicatorVisibility = Visibility.Visible;
            }), DispatcherPriority.Normal);
        }

        private void AiAgent_OnUserSpeechFinished(object? sender, string e)
        {
            _ = Dispatcher.InvokeAsync(new Action(() =>
            {
                tkChat.TypingIndicatorText = "FMS AI is thinking...";
                tkChat.TypingIndicatorVisibility = Visibility.Visible;
            }), DispatcherPriority.Normal);
        }

        private AudioVisualization InitializeAudioVisualAI()
        {
            AudioVisualization audioVisualization = new AudioVisualization();
            #region audioVisualization
            audioVisualization.AudioSource = null;
            audioVisualization.BackColor = System.Drawing.Color.Blue;
            audioVisualization.BarCount = 50;
            audioVisualization.BarSpacing = 2;
            audioVisualization.ColorBase = System.Drawing.Color.Green;
            audioVisualization.ColorMax = System.Drawing.Color.Red;
            audioVisualization.DeviceIndex = 0;
            audioVisualization.FileName = null;
            audioVisualization.HighQuality = true;
            audioVisualization.Interval = 40;
            audioVisualization.IsXLogScale = true;
            audioVisualization.Location = new System.Drawing.Point(0, 0);
            audioVisualization.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            audioVisualization.MaximumFrequency = 10000;
            audioVisualization.MessageArgs = null;
            audioVisualization.Name = "audioVisualizationAI";
            audioVisualization.pic3DGraph = null;
            audioVisualization.Size = new System.Drawing.Size(883, 450);
            audioVisualization.TabIndex = 19;
            audioVisualization.UseAverage = true;
            audioVisualization.UserKey = "Your registration key";
            audioVisualization.UserName = "Your email";
            audioVisualization.VisMode = CSAudioVisualization.GraphMode.ModeSpectrum;
            audioVisualization.Visible = true;
            #endregion

            wsAI.Child = audioVisualization;

            //Set the mode:
            audioVisualization.Mode = CSAudioVisualization.Mode.WasapiLoopbackCapture;

            //Set the device index:
            audioVisualization.DeviceIndex = audioVisualization.GetDeviceDefaultIndex(CSAudioVisualization.Mode.WasapiLoopbackCapture);

            //Set the quality:
            audioVisualization.HighQuality = true;

            //Set the interval:
            audioVisualization.Interval = 40;

            //Set the background color:
            audioVisualization.BackColor = System.Drawing.Color.Black;

            //Set the base color:
            audioVisualization.ColorBase = System.Drawing.Color.Green;

            //Set the max color:
            audioVisualization.ColorMax = System.Drawing.Color.DarkGreen;
            return audioVisualization;
        }
        private AudioVisualization InitializeAudioVisualUser()
        {
            AudioVisualization audioVisualization = new AudioVisualization();
            #region audioVisualization
            audioVisualization.AudioSource = null;
            audioVisualization.BackColor = System.Drawing.Color.Blue;
            audioVisualization.BarCount = 50;
            audioVisualization.BarSpacing = 2;
            audioVisualization.ColorBase = System.Drawing.Color.Green;
            audioVisualization.ColorMax = System.Drawing.Color.Red;
            audioVisualization.DeviceIndex = 0;
            audioVisualization.FileName = null;
            audioVisualization.HighQuality = true;
            audioVisualization.Interval = 40;
            audioVisualization.IsXLogScale = true;
            audioVisualization.Location = new System.Drawing.Point(0, 0);
            audioVisualization.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            audioVisualization.MaximumFrequency = 10000;
            audioVisualization.MessageArgs = null;
            audioVisualization.Name = "audioVisualizationUser";
            audioVisualization.pic3DGraph = null;
            audioVisualization.Size = new System.Drawing.Size(883, 450);
            audioVisualization.TabIndex = 19;
            audioVisualization.UseAverage = true;
            audioVisualization.UserKey = "Your registration key";
            audioVisualization.UserName = "Your email";
            audioVisualization.VisMode = CSAudioVisualization.GraphMode.ModeSpectrum;
            audioVisualization.Visible = true;
            #endregion

            wsUser.Child = audioVisualization;

            //Set the mode:
            audioVisualization.Mode = CSAudioVisualization.Mode.WasapiCapture;

            //Set the device index:
            audioVisualization.DeviceIndex = audioVisualization.GetDeviceDefaultIndex(CSAudioVisualization.Mode.WasapiCapture);

            //Set the quality:
            audioVisualization.HighQuality = true;

            //Set the interval:
            audioVisualization.Interval = 40;

            //Set the background color:
            audioVisualization.BackColor = System.Drawing.Color.Black;

            //Set the base color:
            audioVisualization.ColorBase = System.Drawing.Color.Yellow;

            //Set the max color:
            audioVisualization.ColorMax = System.Drawing.Color.Gold;
            return audioVisualization;
        }

        private async void AiAgent_OnUserMessageReceived(object? sender, ConversationInputTranscriptionFinishedUpdate e)
        {
            _ = Dispatcher.InvokeAsync(new Action(() =>
            {
                if (tkChat.LastMessage == null || tkChat.LastMessage.Message.Author != currentAuthor)
                {
                    tkChat.AddMessage(currentAuthor, e.Transcript);
                }
                else
                {
                    TextMessage? tm = tkChat.LastMessage?.Message as TextMessage;
                    if (tm != null && tm.Author == currentAuthor)
                    {
                        tm.Text += e.Transcript;
                    }
                }
                tkChat.TypingIndicatorVisibility = Visibility.Collapsed;
            }), DispatcherPriority.Normal);
        }

        private async void AiAgent_OnAiMessageReceived(object? sender, ConversationItemStreamingPartDeltaUpdate e)
        {
            _ = Dispatcher.InvokeAsync(new Action(() =>
             {
                 if (tkChat.LastMessage == null || tkChat.LastMessage.Message.Author != aiAuthor)
                 {
                     tkChat.AddMessage(aiAuthor, e.AudioTranscript);
                 }
                 else
                 {
                     TextMessage? tm = tkChat.LastMessage?.Message as TextMessage;
                     if (tm != null && tm.Author == aiAuthor)
                     {
                         tm.Text += e.AudioTranscript;
                     }
                 }
                 tkChat.TypingIndicatorVisibility = Visibility.Collapsed;
             }), DispatcherPriority.Normal);
        }

        private void btnLoad3D_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("111");
            ai3DWindow.Show();


        }
        private void btnLog_Click(object sender, RoutedEventArgs e)
        {
            //SetMessage("hello from WPF->unity");
            Console.WriteLine("222");
            string s = ToolsManager.DoSearch2("searchOpenSGDocument", textBox1.Text, aiAgent.chatClient);
            //string s = ToolsManager.DoSearch2("searchDGTDocument", textBox1.Text, aiAgent.chatClient);
            //string s=ToolsManager.DoSearch2("searchFMSDocument", textBox1.Text, aiAgent.chatClient);
            Console.WriteLine($"xxx:{s}");
        }

        private void radToggleButton1_Checked(object sender, RoutedEventArgs e)
        {
            if (aiAgent == null || aiAgent.Mic == null)
                return;
            aiAgent.StartRecording();

            //radToggleButton.Background = Brushes.Green;

            //Start:
            //audioVisualizationAI.Start();
            audioVisualizationUser.Start();
        }

        private void radToggleButton1_Unchecked(object sender, RoutedEventArgs e)
        {
            if (aiAgent == null || aiAgent.Mic == null)
                return;
            aiAgent.StopRecording();
            //radToggleButton.Background = Brushes.Gray;

            //Stop:
            //audioVisualizationAI.Start();
            audioVisualizationUser.Stop();
        }

        private void radToggleButton2_Checked(object sender, RoutedEventArgs e)
        {
            if (aiAgent == null)
                return;
            aiAgent.StartSpeaking();

            //radToggleButton.Background = Brushes.Green;

            //Start:
            audioVisualizationAI.Start();
            //audioVisualizationUser.Start();
        }

        private void radToggleButton2_Unchecked(object sender, RoutedEventArgs e)
        {
            if (aiAgent == null)
                return;
            aiAgent.StopSpeaking();
            //radToggleButton.Background = Brushes.Gray;

            //Stop:
            audioVisualizationAI.Stop();
            //audioVisualizationUser.Stop();
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Height = 100 + 50;
        }

        private void tkChat_SendMessage(object sender, SendMessageEventArgs e)
        {
            if (aiAgent == null)
                return;
            TextMessage? textMessage = e.Message as TextMessage;
            aiAgent.SendInputTextMessage(textMessage.Text);
        }
    }
}
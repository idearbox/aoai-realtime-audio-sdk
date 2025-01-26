using System;
using System.Collections.Generic;
using System.Linq;
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

namespace RealtimeInteractiveWPFApp
{
    /// <summary>
    /// AI3DWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AI3DWindow : Window
    {
        public AI3DWindow()
        {
            IntPtr hWnd = new WindowInteropHelper(this).EnsureHandle();
            int[] colorstr = new int[] { 0x111111 };
            MainWindow.DwmSetWindowAttribute(hWnd, MainWindow.DWWMA_CAPTION_COLOR, colorstr, 4);

            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}

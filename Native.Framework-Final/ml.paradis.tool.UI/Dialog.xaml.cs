using MaterialDesignExtensions.Controls;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ml.paradis.tool.UI
{
    /// <summary>
    /// Dialog.xaml 的交互逻辑
    /// </summary>
    public partial class Dialog :  MaterialWindow
    {
        public static void ShowDialog(string titleStr, object subtitleContent, object ButtonLeftContent)
        {
            Dialog dialog = new Dialog();
            dialog.title.Text = titleStr;
            dialog.Subtitle.Content = subtitleContent;
            dialog.ButtonLeft.Content = ButtonLeftContent;
            dialog.ShowDialog();
            dialog.Activate();
            _ = Task.Run(() =>
            {
                System.Threading.Thread.Sleep(1000);
                dialog.Width =  Math.Max(dialog.ButtonLeft.ActualWidth, dialog.Subtitle.ActualWidth);
            });
        }
        public Dialog()
        {
            InitializeComponent();
        }

        private void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

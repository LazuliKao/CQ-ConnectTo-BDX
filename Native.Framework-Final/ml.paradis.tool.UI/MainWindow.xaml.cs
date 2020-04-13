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
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace ml.paradis.tool.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class    MainWindow : MaterialDesignExtensions.Controls.MaterialWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Frame.Navigate(new welcome());
        }
        private void LoadPage(Page page)
        {
            if ((Page)Frame.Content is wsc)
            {
                ((wsc)Frame.Content).UnInstall();
            }
            ((Page)Frame.Content).Content = null;
            Frame.Navigate(page);
        }
        private void TabRadioButtonWSC_Checked(object sender, RoutedEventArgs e)
        {
            LoadPage(new wsc());
        }
        private void TabRadioButtonConfig_Checked(object sender, RoutedEventArgs e)
        {
           LoadPage(new config());
        }
        private void TabRadioButtonAbout_Checked(object sender, RoutedEventArgs e)
        {
            LoadPage(new update());
        }
    }
}

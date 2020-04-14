using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Native.Tool.Http;
namespace ml.paradis.tool.UI
{
    /// <summary>
    /// update.xaml 的交互逻辑
    /// </summary>
    public partial class update : Page
    {
        public update()
        {
            InitializeComponent();
            //web.Source =new Uri( "https://github.com/littlegao233/MinecraftToolKit");
            // HttpHelper.GetHtmlStr("https://github.com/littlegao233/MinecraftToolKit");

            // web.Source = new Uri("https://baidu.com");
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e) => Process.Start("https://github.com/littlegao233/CQ-ConnectTo-BDX/");
        private void McBBSButton_Click(object sender, RoutedEventArgs e) => Process.Start("https://www.mcbbs.net/thread-1011364-1-1.html");
        private void MinebbsButton_Click(object sender, RoutedEventArgs e) => Process.Start("https://www.minebbs.com/resources/1023/");
        private void CQPButton_Click(object sender, RoutedEventArgs e) => Process.Start("https://cqp.cc/t/49225");

        private void UpdateTitle_Loaded(object sender, RoutedEventArgs e)
        {
            //Native.Tool.Http.HttpWebClient webClient = new HttpWebClient();
            //webClient.downloadf
            //Task.Run(() =>
            //{
            //    string get = HttpStringGet.GetHtmlStr("https://github.com/littlegao233/CQ-ConnectTo-BDX/releases");
            //    Dispatcher.Invoke(() => UpdateOut.Text += get);
            //});

        }

        private void UpdateWB_LoadCompleted(object sender, NavigationEventArgs e)
        {
            var html = UpdateWB.Document as HtmlDocument;

            UpdateOut.Text += html.GetElementById("tag-select-menu-5e27c41e-7d3d-11ea-8ca6-9e2d60861627").Parent.InnerText;
            UpdateWB.Visibility = Visibility.Collapsed;

        }

        private void UpdateWB_Unloaded(object sender, RoutedEventArgs e)
        {
            var html = UpdateWB.Document as HtmlDocument;

            UpdateOut.Text += html.GetElementById("tag-select-menu-5e27c41e-7d3d-11ea-8ca6-9e2d60861627").Parent.InnerText;
            UpdateWB.Visibility = Visibility.Collapsed;
        }
    }
}

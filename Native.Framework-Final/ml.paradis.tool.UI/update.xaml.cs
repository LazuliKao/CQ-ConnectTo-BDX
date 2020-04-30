using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Native.Sdk.Cqp;
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
            string versonN = "v0.1.2";
            Task.Run(() =>
            {
                HttpWebClient webClient = new HttpWebClient();
                string download = webClient.DownloadString("https://github.com/littlegao233/CQ-ConnectTo-BDX/releases/latest");
                var relase_link = Regex.Match(download, "<body>You are being <a href=\"https://github.com/littlegao233/CQ-ConnectTo-BDX/releases/tag/(?<version>.*?)\">redirected</a>.");
                Dispatcher.Invoke(() => UpdateOut.Clear());
                Dispatcher.Invoke(() => UpdateTitle.Text = $"最新版本:{relase_link.Groups["version"].Value}\n当前版本:{versonN}");
                download = webClient.DownloadString("https://github.com/littlegao233/CQ-ConnectTo-BDX/releases/tag/" + relase_link.Groups["version"].Value);

                Dispatcher.Invoke(() => UpdateOut.Text += download);

            });
            //webClient.downloadf
            //Task.Run(() =>
            //{
            //    string get = HttpStringGet.GetHtmlStr("https://cqp.cc/t/49225"
            //        , Encoding.UTF8  );
            //    Dispatcher.Invoke(() => UpdateOut.Text += get);
            //});

        }


    }
}

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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;
using Native.Sdk.Cqp;
using Native.Tool.Http;
namespace ml.paradis.tool.UI
{
    /// <summary>
    /// update.xaml 的交互逻辑
    /// </summary>
    public partial class update : Page
    {
        public update() => InitializeComponent();
        private void GitHubButton_Click(object sender, RoutedEventArgs e) => Process.Start("https://github.com/littlegao233/CQ-ConnectTo-BDX/");
        private void McBBSButton_Click(object sender, RoutedEventArgs e) => Process.Start("https://www.mcbbs.net/thread-1011364-1-1.html");
        private void MinebbsButton_Click(object sender, RoutedEventArgs e) => Process.Start("https://www.minebbs.com/resources/1023/");
        private void CQPButton_Click(object sender, RoutedEventArgs e) => Process.Start("https://cqp.cc/t/49225");

        private void UpdateTitle_Loaded(object sender, RoutedEventArgs e) => UpdateRefresh();
        private System.Timers.Timer Refreshing = new System.Timers.Timer(10000) { AutoReset = false, Enabled = false };
        private bool? RefreshingBusy = null;
        private Task UpdateQueryTask;
        private void UpdateRefresh()
        {
            if (RefreshingBusy != true)
            {
                string versonN = "v0.1.2";
                if (RefreshingBusy == null)
                {
                    Refreshing.Elapsed += (sender, e) =>
                    {
                        RefreshingBusy = false; CheckRefreshBusy();
                    };
                }
                RefreshingBusy = true; CheckRefreshBusy();
                Refreshing.Stop();
                Refreshing.Start();
                UpdateLog.Children.Clear();
                UpdateLog.Children.Add(new TextBlock() { FontSize = 20, Text = $"当前版本:{versonN}" });
                UpdateLog.Children.Add(new TextBlock() { FontSize = 20, Foreground = Brushes.Red, Text = $"正在获取最新版本..." });
                int taskID = 0;
                UpdateQueryTask = Task.Run(() =>
                {
                    try
                    {
                        HttpWebClient webClient = new HttpWebClient();
                        webClient.Encoding = Encoding.UTF8;
                        var download = webClient.DownloadData("https://github.com/littlegao233/CQ-ConnectTo-BDX/releases/latest");
                        var relase_link = Regex.Match(Encoding.UTF8.GetString(download), "<body>You are being <a href=\"https://github.com/littlegao233/CQ-ConnectTo-BDX/releases/tag/(?<version>.*?)\">redirected</a>.");
                        Dispatcher.Invoke(() => UpdateLog.Children.Clear());
                        Dispatcher.Invoke(() => UpdateLog.Children.Add(new TextBlock() { FontSize = 20, Text = $"最新版本:{relase_link.Groups["version"].Value}\n当前版本:{versonN}{(relase_link.Groups["version"].Value == versonN ? "" : "\n请及时更新体验最新bug")}" }));
                        //download = webClient.DownloadData("https://github.com/littlegao233/CQ-ConnectTo-BDX/releases/");
                        string GetVersionMD(string uri)
                        {
                            return Regex.Match(Encoding.UTF8.GetString(webClient.DownloadData(uri)), "<div class=\"markdown-body\">(?<content>(.*|\\s*)*?)</div>").Groups["content"].Value;
                        }
                        UIElement ParseHtml(string html)
                        {
                            StackPanel stackPanel = new StackPanel();
                            var match = Regex.Match(html, @"<(?<type>..)>(?<content>(.|\s)*?)</\k<type>>");
                            while (match.Success)
                            {
                                switch (match.Groups["type"].Value)
                                {
                                    case "h1":
                                    case "h2":
                                    case "h3":
                                    case "h4":
                                    case "h5":
                                    case "h6":
                                    case "h7":
                                        int TextType = int.Parse(Regex.Replace(match.Groups["type"].Value, "^h", ""));
                                        stackPanel.Children.Add(new TextBlock() { Margin = new Thickness(TextType * 5, 0, 0, 0), Foreground = new SolidColorBrush(Color.FromRgb(0, (byte)Math.Min(255, 30 * TextType), (byte)Math.Max(10, 255 - 30 * TextType))), Text = match.Groups["content"].Value, FontSize = 24 - 2 * TextType });
                                        break;
                                    case "li":
                                        Match getLink = Regex.Match(match.Groups["content"].Value, "<a.*?alt=\"(?<content>.*?)\".*?</a>");
                                        string getText = getLink.Success ? match.Groups["content"].Value.Replace(getLink.Value, getLink.Groups["content"].Value) : match.Groups["content"].Value;
                                        getLink = Regex.Match(match.Groups["content"].Value, "<a.*?rel=\"nofollow\">(?<content>.*?)</a>");
                                        getText = getLink.Success ? getText.Replace(getLink.Value, getLink.Groups["content"].Value) : getText;
                                        Match Cont = Regex.Match(getText, @"<(?<type>..)>(?<content>(.|\s)*?)</\k<type>>");
                                        if (Cont.Success)
                                        {
                                            stackPanel.Children.Add(ParseHtml(getText));
                                        }
                                        else
                                        {
                                            stackPanel.Children.Add(new TextBlock() { Text = getText, FontSize = 13 });
                                        }
                                        break;
                                    case "ul":
                                        stackPanel.Children.Add(ParseHtml(match.Groups["content"].Value));
                                        break;
                                    default:
                                        stackPanel.Children.Add(ParseHtml(match.Groups["content"].Value));
                                        break;
                                }
                                html = html.Replace(match.Value, "");
                                match = Regex.Match(html, @"<(?<type>..)>(?<content>(.|\s)*?)</\k<type>>");
                            }
                            return stackPanel;
                        }
                        foreach (Match match in Regex.Matches(
                            Encoding.UTF8.GetString(webClient.DownloadData("https://github.com/littlegao233/CQ-ConnectTo-BDX/tags")),
                            @"<a href=""/littlegao233/CQ-ConnectTo-BDX/releases/tag/(.*?)"">\s*\1\s*</a>"))
                        {
                            string VersionTag = match.Groups[1].Value;
                            //headDockPanel.Children.Add(new Button() {Margin=new Thickness(0), FontSize = 20, Text = $"{VersionTag}更新日志", Foreground = Brushes.White });
                            string HTMLDoc = GetVersionMD("https://github.com/littlegao233/CQ-ConnectTo-BDX/releases/tag/" + VersionTag);
                            if (UpdateQueryTask.Id != taskID) { return; }
                            Dispatcher.Invoke(() =>
                            {
                                Grid headPanel = new Grid() { Background = new SolidColorBrush(Color.FromArgb(versonN == VersionTag ? (byte)0 : (byte)100, 100, 255, int.Parse(Regex.Replace(versonN, "[^\\d]", "")) >= int.Parse(Regex.Replace(VersionTag, "[^\\d]", "")) ? (byte)255 : (byte)0)) };
                                headPanel.Children.Add(new TextBlock() { Margin = new Thickness(8), FontSize = 20, Text = $"{VersionTag}更新日志", Foreground = Brushes.White });
                                ((Button)headPanel.Children[
                                      headPanel.Children.Add(new Button() { Content = new PackIcon() { Padding = new Thickness(0), Kind = PackIconKind.OpenInNew, Width = 25, Height = 25 }, Foreground = Brushes.White, Style = Resources["MaterialDesignFlatButton"] as Style, HorizontalAlignment = HorizontalAlignment.Right })
                                 ]).Click += (sender, e) => Process.Start("https://github.com/littlegao233/CQ-ConnectTo-BDX/releases/tag/" + VersionTag);
                                _ = UpdateLog.Children.Add(new GroupBox()
                                {
                                    Style = Resources["MaterialDesignCardGroupBox"] as Style,
                                    Header = headPanel,
                                    Content = new Label() { Content = ParseHtml(HTMLDoc) },
                                    Padding = new Thickness(0),
                                    Margin = new Thickness(5)
                                });
                            }
                             );
                            RefreshingBusy = false; CheckRefreshBusy();
                        }
                    }
                    catch (Exception err)
                    {
                        Dispatcher.Invoke(() => UpdateLog.Children.Add(new TextBlock() { FontSize = 20, Text = $"更新获取失败!!!\n请重试...\n{err.Message}" }));
                        //Dispatcher.Invoke(() => UpdateLog.Children.Add(new Button() { FontSize = 20, Text = "更新获取失败!!!\n请重试..." }));
                    }
                }
                );
                taskID = UpdateQueryTask.Id;
            }
        }
        private void CheckRefreshBusy()
        {
            if (RefreshingBusy == true)
            {
                Dispatcher.Invoke(() =>
                {
                    CheckUpdateButton.IsEnabled = false;
                    CheckUpdateButton.Content = "获取中   请稍候...";
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    CheckUpdateButton.IsEnabled = true;
                    CheckUpdateButton.Content = "点击检查更新";
                });
            }
        }
        private void CheckUpdateButton_Click(object sender, RoutedEventArgs e) => UpdateRefresh();
    }
}

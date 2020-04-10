using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
using WebSocketSharp;


namespace MinecraftToolKit.Pages
{
    public class SelectedToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value >= 0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
    //public class SelectedClientToBool : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    { 
    //            return ((Tesws.WS)((ComboBoxItem)value).Tag).client.IsAlive; 
    //    }
    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    //}
    /// <summary>
    /// tesws.xaml 的交互逻辑
    /// </summary>
    public partial class Tesws : Page
    {
        public Tesws()
        {
            InitializeComponent();
            if (File.Exists(Environment.CurrentDirectory + "\\tesws.json"))
            {
                OutPut($"检测到位于{Environment.CurrentDirectory}\\tesws.json的配置文件");
                try
                {
                    foreach (JObject item in JObject.Parse(File.ReadAllText(Environment.CurrentDirectory + "\\tesws.json"))["Servers"])
                    {
                        WebSocket webSocket = new WebSocket(item["Address"].ToString());
                        webSocket.OnOpen += (senderClient, ClientE) =>
                        {
                            OutPut($"{(senderClient as WebSocket).Url}已连接");
                        };
                        webSocket.OnError += (senderClient, ClientE) =>
                        {
                            OutPut($"{(senderClient as WebSocket).Url}出错啦{ClientE.Message}");
                        };
                        webSocket.OnMessage += WebSocket_OnMessage;
                        webSocket.OnClose += (senderClient, ClientE) =>
                        {
                            OutPut($"{(senderClient as WebSocket).Url}连接已关闭\t{ClientE.Code}=>{ClientE.Reason}");
                        };
                        SelectServer.Items.Add(new ComboBoxItem() { Content = item["Address"].ToString(), Tag = new WS() { client = webSocket, info = item } });
                    }
                    OutPut("配置文件读取成功");
                }
                catch (Exception err)
                { OutPut("配置文件读取失败" + err.Message); }
            }
        }
        #region 艹
        private void AddServerButton_Click(object sender, RoutedEventArgs e)
        {
            //DialogContent.Children.Clear();
            //string get = null;
            AddAddress.Clear();
            AddPWD.Clear();
            AddTip.Visibility = Visibility.Collapsed;
            Dialog.IsOpen = true;

            //DialogContent.Children.Add(new Button() {});

        }
        #endregion
        private void RemoveServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (((WS)((ComboBoxItem)SelectServer.SelectedItem).Tag).client.IsAlive)
            {
                ((WS)((ComboBoxItem)SelectServer.SelectedItem).Tag).client.CloseAsync();
            };
            SelectServer.Items.Remove(SelectServer.SelectedItem);
        }
        public static JObject GetCmdReq(string token, string cmd)
        {
            string GetMD5(string sDataIn)
            {
                System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] bytValue, bytHash;
                bytValue = Encoding.UTF8.GetBytes(sDataIn);
                bytHash = md5.ComputeHash(bytValue);
                md5.Clear();
                string sTemp = "";
                for (int i = 0; i < bytHash.Length; i++)
                {
                    sTemp += bytHash[i].ToString("X").PadLeft(2, '0');
                }
                return sTemp.ToUpper();
            }
            JObject raw = JObject.Parse("{\"op\":\"runcmd\",\"passwd\":\"token\",\"cmd\":\"say null\"}");
            raw["cmd"] = cmd;
            raw["passwd"] = GetMD5(token + DateTime.Now.ToString("yyyyMMddHHmm"));
            return raw;
        }
        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectServer.SelectedIndex != -1)
            {
                if (((WS)((ComboBoxItem)SelectServer.SelectedItem).Tag).client.IsAlive)
                {
                    switch (SelectAction.SelectedIndex)
                    {
                        case 0:
                            string getStr = SendText.Text.Replace("%pwd%", GetCmdReq(((WS)((ComboBoxItem)SelectServer.SelectedItem).Tag).info["Password"].ToString(), null)["passwd"].ToString());
                            ((WS)((ComboBoxItem)SelectServer.SelectedItem).Tag).client.SendAsync(getStr, (state) => OutPut(state ? "发送成功=>" + SendText.Text : "发送失败!?"));
                            OutPut(getStr);
                            break;
                        case 1:
                            string cmdStr = GetCmdReq(((WS)((ComboBoxItem)SelectServer.SelectedItem).Tag).info["Password"].ToString(), SendText.Text).ToString(Newtonsoft.Json.Formatting.None);
                            ((WS)((ComboBoxItem)SelectServer.SelectedItem).Tag).client.SendAsync(cmdStr, (state) => OutPut(state ? "发送成功=>" + cmdStr : "发送失败!?"));
                            //SendText.Text 
                            break;
                        default:
                            OutPut("还没选择消息内容类别啊啊啊啊啊啊！！！");
                            break;
                    }
                }
                else
                {
                    OutPut("还没连接啊啊啊啊啊啊！！！");
                }
            }
        }
        public struct WS
        {
            public WebSocket client;
            public JObject info;
        }
        private void OutPut(object input) => Dispatcher.Invoke(() => { OutPutText.AppendText(DateTime.Now.ToString() + ">" + input + Environment.NewLine); OutPutText.ScrollToEnd(); });
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WebSocket webSocket = new WebSocket(AddAddress.Text);
                webSocket.OnOpen += (senderClient, ClientE) =>
                {
                    OutPut($"{(senderClient as WebSocket).Url}已连接");
                };
                webSocket.OnError += (senderClient, ClientE) =>
               {
                   OutPut($"{(senderClient as WebSocket).Url}出错啦{ClientE.Message}");
               };
                webSocket.OnMessage += WebSocket_OnMessage;
                webSocket.OnClose += (senderClient, ClientE) =>
                {
                    OutPut($"{(senderClient as WebSocket).Url}连接已关闭\t{ClientE.Code}=>{ClientE.Reason}");
                };
                SelectServer.Items.Add(new ComboBoxItem() { Content = AddAddress.Text, Tag = new WS() { client = webSocket, info = new JObject() { new JProperty("Address", AddAddress.Text), new JProperty("Password", AddPWD.Text) } } });
                if (SelectServer.SelectedIndex == -1)
                {
                    SelectServer.SelectedIndex = 0;
                }
                Dialog.IsOpen = false;
            }
            catch (Exception err)
            {
                AddTip.Visibility = Visibility.Visible;
                AddTip.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                AddTip.Text = "填写内容不合规\n" +
                 err.Message;
            }
        }

        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            OutPut($"{(sender as WebSocket).Url}收信:{e.Data}");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Dialog.IsOpen = false;
        }
        private void SelectServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectServer.SelectedIndex != -1)
            {
                if (((WS)((ComboBoxItem)SelectServer.SelectedItem).Tag).client.IsAlive)
                {
                    OutPut("已经连上了啊啊啊啊啊啊！！！");
                }
                else
                {
                    ((WS)((ComboBoxItem)SelectServer.SelectedItem).Tag).client.ConnectAsync();
                }
            }
            else
            {
                OutPut("把你想要发送的Server选上再点啊啊啊啊啊啊！！！");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject jObject = new JObject() { new JProperty("Servers", new JArray()) };
                foreach (var item in SelectServer.Items)
                {
                    ((JArray)jObject["Servers"]).Add(((WS)((ComboBoxItem)item).Tag).info);
                }
                File.WriteAllText(Environment.CurrentDirectory + "\\tesws.json", jObject.ToString());
                OutPut("配置文件已保存到" + Environment.CurrentDirectory + "\\tesws.json");
            }
            catch (Exception) { }
        }
    }
}

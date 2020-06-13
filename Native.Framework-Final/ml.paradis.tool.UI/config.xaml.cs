using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignExtensions.Controls;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;

namespace ml.paradis.tool.UI
{
    /// <summary>
    /// config.xaml 的交互逻辑
    /// </summary>
    public partial class config : Page
    {
        public config()
        {
            InitializeComponent();
            WSservers.Add(new WSModel());
            WSDataGrid.ItemsSource = WSservers;
        }
        private void OpenConfigFilePathButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("Explorer.exe", $" /select, {Code.Data.ConfigPath}");
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {


                #region 填写检查  
                void ShowErr(TextBlock tb, string errM)
                {
                    tb.Text = errM;
                    tb.Visibility = Visibility.Visible;
                }
                void ShowErrFTB(TextBlock tx, TextBox tb, string errM)
                {
                    ShowErr(tx, errM);
                    tb.Focus();
                }
                #region 1
                ErrLabel_1.Visibility = Visibility.Collapsed;
                foreach (var wss in WSservers)
                {
                    try
                    { _ = new WebSocketSharp.WebSocket(wss.Address); }
                    catch (Exception err)
                    {
                        ShowErr(ErrLabel_1, $"地址填写不合规\n位于{wss.Order}\n" + err.Message); return;
                    }
                    if (string.IsNullOrEmpty(wss.Password))
                    {
                        ShowErr(ErrLabel_1, "把密码填上啊啊啊啊啊!!!\n位于" + wss.Order); return;
                    }
                    if (string.IsNullOrEmpty(wss.Name))
                    {
                        ShowErr(ErrLabel_1, "把服务器名称(标识)填上啊啊啊啊啊!!!\n位于" + wss.Order); return;
                    }
                }
                #endregion
                #region 2
                ErrLabel_2.Visibility = Visibility.Collapsed;
                if (!Regex.IsMatch(GroupID_2.Text, "^\\d{4,12}$"))
                {
                    ShowErrFTB(ErrLabel_2, GroupID_2, "群号码格式有误!!!"); return;
                }
                #endregion
                #region 3
                ErrLabel_3.Visibility = Visibility.Collapsed;
                if (AdminMode_3.IsChecked == false)
                {
                    if (!Regex.IsMatch(AdminQQ_3.Text, @"^(\d{4,13}(\\|/|\|)?)+$"))
                    {
                        ShowErrFTB(ErrLabel_3, AdminQQ_3, "请正确填写QQ号!!!(多个管理QQ请用\"|\"分隔)"); return;
                    }
                }
                #endregion
                #region 4 
                ErrLabel_4.Visibility = Visibility.Collapsed;
                if (Func_4_TimerQueryCB.IsChecked == true)
                {
                    try
                    {
                        if (int.Parse(Func_4_TimerQueryDuration.Text) < 10)
                        {
                            ShowErrFTB(ErrLabel_4, Func_4_TimerQueryDuration, "不合理的查询间隔!!!"); return;
                        }
                    }
                    catch (Exception) { ShowErrFTB(ErrLabel_4, Func_4_TimerQueryDuration, "请正确填写查询间隔!!!"); return; }
                }
                #endregion
                #endregion
                string configStr = Encoding.UTF8.GetString(Code.Config.config);
                configStr = configStr.Replace("386475891", GroupID_2.Text);
                if (AdminMode_3.IsChecked == false)
                {
                    JArray replacement = new JArray();
                    foreach (var item in AdminQQ_3.Text.Split('|', '/'))
                    {
                        replacement.Add(new JObject() {
                        new JProperty("Path", new JArray(){ "FromQQ"}) ,
                        new JProperty("Operator", "==") ,
                        new JProperty("Value", item) ,
                    });
                    }
                    configStr = Regex.Replace(configStr, "//##START_ReplacePlace_1##(.+\\s+)+?//##END_ReplacePlace_1##", "\"any_of\":" + replacement.ToString());
                }
                string ServerNames = string.Join("|", WSservers.ToList().ConvertAll(l => l.Name));
                if (ServerNames.Length > 1)
                {
                    configStr = configStr.Replace("测试服务器1|测试服务器2", ServerNames);
                }
                #region 服务器配置
                JObject config = JObject.Parse(configStr);
                string serverStr = ((JObject)config["Servers"][0]).ToString();
                config["Servers"][0].Remove();
                foreach (WSModel wss in WSservers)
                {
                    //server.Replace();
                    var server = JObject.Parse(serverStr);
                    server["Address"] = wss.Address;
                    server["Passwd"] = wss.Password;
                    server["Tag"] = wss.Name;
                    ((JArray)config["Servers"]).Add(server);
                }
                if (Func_4_TimerQueryCB.IsChecked == true)
                {
                    config["Timers"][0]["Interval"] = int.Parse(Func_4_TimerQueryDuration.Text);
                }
                else
                {
                    config["Timers"] = new JArray();
                }
                #endregion
                //END
                StackPanel stackPanel = new StackPanel();
                #region 写入文件
                try
                {
                    if (File.Exists(Code.Data.ConfigPath))
                    {
                        File.Copy(Code.Data.ConfigPath, Path.GetDirectoryName(Code.Data.ConfigPath) + "\\config_old.json");
                        stackPanel.Children.Add(new TextBlock() { Text = "旧文件已自动备份到" });
                        stackPanel.Children.Add(new TextBox() { Text = Path.GetDirectoryName(Code.Data.ConfigPath) + "\\config_old.json", IsReadOnly = true });
                    }
                }
                catch (Exception)
                {
                    for (int i = 1; i < 500; i++)
                    {
                        try
                        {
                            File.Copy(Code.Data.ConfigPath, Path.GetDirectoryName(Code.Data.ConfigPath) + "\\config_old (" + i + ").json");
                            stackPanel.Children.Add(new TextBlock() { Text = "旧文件已自动备份到" });
                            stackPanel.Children.Add(new TextBox() { Text = Path.GetDirectoryName(Code.Data.ConfigPath) + "\\config_old (" + i + ").json", IsReadOnly = true });
                            break;
                        }
                        catch (Exception)
                        { continue; }
                    }
                }
                File.WriteAllText(Code.Data.ConfigPath, config.ToString());
                #endregion
                Button EXbutton = new Button() { Content = "打开配置文件目录"/*, Margin = new Thickness(0)*/ };
                EXbutton.Click += (sender1, e1) =>
                {
                    OpenConfigFilePathButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    ((Button)sender1).Content = "已打开";
                    ((Button)sender1).IsEnabled = false;
                };
                stackPanel.Children.Add(new TextBlock() { Text = "\n配置文件已保存到" });
                stackPanel.Children.Add(new TextBox() { Text = Code.Data.ConfigPath, IsReadOnly = true });
                stackPanel.Children.Add(new TextBlock() { Text = "请手动重载酷Q插件应用效果" });
                Dialog.ShowDialog("保存成功,请手动重载插件", stackPanel, EXbutton);
            }
            catch (Exception err)
            { Dialog.ShowDialog("遭遇错误\n调试信息:", err.ToString(), "行！"); }
        }

        private void GuideTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(() =>
              {
                  System.Threading.Thread.Sleep(200);
                  Dispatcher.Invoke(() => ((TextBox)sender).SelectAll());
              });
        }
        #region 数据
        ObservableCollection<WSModel> WSservers = new ObservableCollection<WSModel>();
        private void AddWSSButton_Click(object sender, RoutedEventArgs e)
        {
            WSservers.Add(new WSModel());
            SortWs();
        }
        private void RemoveWSSButton_Click(object sender, RoutedEventArgs e)
        {
            WSservers.RemoveAt(int.Parse((sender as Button).Tag.ToString().Substring(1)) - 1);
            SortWs();
        }
        private void SortWs()
        {
            for (int i = 0; i < WSservers.Count; i++)
            {
                WSservers[i].Order = "#" + (i + 1).ToString();
            }
        }
        #region TextBox编辑更新
        private void WSSTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var edit = ((WSModel)WSDataGrid.SelectedItem);
                WSservers[int.Parse(edit.Order.Substring(1)) - 1].Address = ((TextBox)sender).Text;
            }
            catch (Exception) { }
        }
        private void PasswdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var edit = ((WSModel)WSDataGrid.SelectedItem);
                WSservers[int.Parse(edit.Order.Substring(1)) - 1].Password = ((TextBox)sender).Text;
            }
            catch (Exception) { }
        }
        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var edit = ((WSModel)WSDataGrid.SelectedItem);
                WSservers[int.Parse(edit.Order.Substring(1)) - 1].Name = ((TextBox)sender).Text;
            }
            catch (Exception) { }
        }
        #endregion
        #endregion
    }
    public class WSModel : INotifyPropertyChanged
    {
        private string _Address = null;
        public string Address
        {
            get { return _Address; }
            set { _Address = value; FirePropertyChanged("Address"); }
        }
        private string _Password = null;
        public string Password
        {
            get { return _Password; }
            set { _Password = value; FirePropertyChanged("Password"); }
        }
        private string _Order = "#1";
        public string Order
        {
            get { return _Order; }
            set { _Order = value; FirePropertyChanged("Order"); }
        }
        private string _Name = null;
        public string Name
        {
            get { return _Name; }
            set { _Name = value; FirePropertyChanged("Order"); }
        }
        public virtual event PropertyChangedEventHandler PropertyChanged;
        public virtual void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

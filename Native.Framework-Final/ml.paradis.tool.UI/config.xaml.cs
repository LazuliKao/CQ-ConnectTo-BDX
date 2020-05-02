using System;
using System.Collections.Generic;
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
        }

        //private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        //{

        //}

        private void OpenConfigFilePathButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("Explorer.exe", $" /select, {Code.Data.ConfigPath}");
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            #region 填写检查  
            void ShowErr(TextBlock tb, string errM)
            {
                tb.Text = errM;
                tb.Visibility = Visibility.Visible;
            }
            void ShowErrFTB(TextBlock tx, TextBox tb, string errM)
            {
                tx.Text = errM;
                tx.Visibility = Visibility.Visible;
                tb.Focus();
            }
            #region 1
            ErrLabel_1.Visibility = Visibility.Collapsed;
            try
            { _ = new WebSocketSharp.WebSocket(Address_1.Text); }
            catch (Exception err)
            {
                ShowErrFTB(ErrLabel_1, Address_1, "地址填写不合规\n" + err.Message); return;
            }
            if (string.IsNullOrEmpty(Password_1.Text))
            {
                ShowErrFTB(ErrLabel_1, Password_1, "把密码填上啊啊啊啊啊!!!"); return;
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
                if (!Regex.IsMatch(AdminQQ_3.Text, "^(\\d|\\|){4,13}"))
                {
                    ShowErrFTB(ErrLabel_3, AdminQQ_3, "请正确填写QQ号!!!(多个管理QQ请用\"|\"分隔)"); return;
                }
            }
            #endregion
            #region 4 
            ErrLabel_4.Visibility = Visibility.Collapsed;
            if (string.IsNullOrEmpty(ServerTag_4.Text))
            {
                ShowErrFTB(ErrLabel_4, ServerTag_4, "把名称填上啊啊啊啊啊!!!"); return;
            }
            #endregion
            #endregion
            string config = Encoding.UTF8.GetString(Code.Config.config);
            Match match = Regex.Match(config, "\"Address\":\\s\"ws://localhost:29132/mc\",(?<space1>\\s*)\"Passwd\":\\s\"passwd\",(?<space2>\\s*)\"Tag\":\\s\"测试服务器1\",");
            if (match.Success)
            {
                config = Regex.Replace(config, match.Value, $"\"Address\": \"{Address_1.Text}\",{match.Groups["space1"]}\"Passwd\": \"{Password_1.Text}\",{match.Groups["space2"]}\"Tag\": \"{ServerTag_4.Text}\",")
                    .Replace("386475891", GroupID_2.Text).Replace("测试服务器1", ServerTag_4.Text);
            }
            if (AdminMode_3.IsChecked == false)
            {
                Match match1 = Regex.Match(config, @"(?<space1>\s*){(?<space2>\s*)""Variant"":\s""MemberType"",\s*""Operator"":\s""=="",\s*.+?//判断是群主\s*},\s*{\s*.*\s*.*\s*.+?//判断是管理员\s*}");
                if (match1.Success)
                {
                    string replacement = null;
                    foreach (var item in AdminQQ_3.Text.Split('|'))
                    {
                        replacement += $"{match1.Groups["space1"].Value}{{{match1.Groups["space2"].Value}\"Path\": [{match1.Groups["space2"].Value}\"FromQQ\"{match1.Groups["space2"].Value}],{match1.Groups["space2"].Value}\"Operator\": \"==\",{match1.Groups["space2"].Value}\"Value\": \"{item}\"//通过QQ号判断权限{match1.Groups["space1"].Value}}},";
                    }
                    config = Regex.Replace(config, match1.Value, replacement.Remove(replacement.Length - 1));
                }
            }
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
            File.WriteAllText(Code.Data.ConfigPath, config);
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

        private void GuideTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(() =>
              {
                  System.Threading.Thread.Sleep(200);
                  Dispatcher.Invoke(() => ((TextBox)sender).SelectAll());
              });
        }
    }
}

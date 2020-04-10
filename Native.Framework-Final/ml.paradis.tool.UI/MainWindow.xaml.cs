using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace Elements.Converters
{
    public class MainWinRectangle : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (bool)value ? new SolidColorBrush(Color.FromArgb(255, 33, 150, 243)) : new SolidColorBrush(Color.FromArgb(255, 158, 158, 158));
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
namespace ml.paradis.tool.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 窗体
        public MainWindow()
        {
            InitializeComponent();
            //Frame.Navigate(new Pages.welcome());
        }
        private void Move_window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Frame.IsEnabled = false;
            headerColorZone.Cursor = Cursors.SizeAll;
            try { DragMove(); }
            catch { }
            Frame.IsEnabled = true;
            headerColorZone.Cursor = Cursors.Arrow;
        }
        #endregion
        //private void MiniWindow_Click(object sender, RoutedEventArgs e)
        //{
        //    WindowState = WindowState.Minimized;
        //}

        //private void CloseWindow_Click(object sender, RoutedEventArgs e)
        //{
        //    //Application.Current.Shutdown();
        //    //this._mainWindow = null;
        //    Close();
        //}



        private void LoadPage(Page page)
        {

            //if ((Page)Frame.Content is Pages.Websocket)
            //{
            //    ((Pages.Websocket)Frame.Content).UnInstall();
            //}
            //((Page)Frame.Content).Content = null;
            //Frame.Navigate(page);
        }
        private void TabRadioButtonHome_Checked(object sender, RoutedEventArgs e)
        {
            //LoadPage(new Pages.home());
        }
        private void TabRadioButtonServers_Checked(object sender, RoutedEventArgs e)
        {
            //LoadPage(new Pages.Servers());
        }
        private void TabRadioButtonHexEdit_Checked(object sender, RoutedEventArgs e)
        {
            //LoadPage(new Pages.HexEdit());
        }
        private void TabRadioButtonUpdate_Checked(object sender, RoutedEventArgs e)
        {
            //LoadPage(new Pages.update());
        }
        private void TabRadioButtonGithub_Checked(object sender, RoutedEventArgs e)
        {
            //LoadPage(new Pages.update());
        }
        private void TabRadioButtonWebsocket_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void TabRadioButtonFloatColor_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void TabRadioButtonWhitelistEdit_Checked(object sender, RoutedEventArgs e)
        {
            //LoadPage(new Pages.whitelistEdit());
        }

        //private void MaxSizeButton_Click(object sender, RoutedEventArgs e)
        //{
        //    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        //}

        //private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    WindowSize.Kind = WindowState == WindowState.Maximized ? MaterialDesignThemes.Wpf.PackIconKind.ArrowCompress : MaterialDesignThemes.Wpf.PackIconKind.Add;
        //    //BorderGroupBox.Margin = WindowState == WindowState.Maximized ? new Thickness(6) : new Thickness(0);
        //}
    }
}

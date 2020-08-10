using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using System;
using System.Threading;

namespace ml.paradis.tool.UI
{
    public class Menu_OpenWindow : IMenuCall
    {
        public void WriteLine(object content)
        {
            Console.Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("CQToWS");
            Console.ForegroundColor = defaultForegroundColor;
            Console.Write("]");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[Main] ");
            ResetConsoleColor();
            Console.WriteLine(content);
        }
        public void WriteLineERR(object type, object content)
        {
            Console.Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("CQToWS");
            Console.ForegroundColor = defaultForegroundColor;
            Console.Write("]");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERROR] ");
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($">{type}<");
            ResetConsoleColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(content);
            ResetConsoleColor();
        }
        private Thread windowthread = null;
        private ManualResetEvent manualResetEvent = null;
        private bool windowOpened = false;
        private MainWindow _mainWindow = null;
        private ConsoleColor defaultForegroundColor = ConsoleColor.White;
        private ConsoleColor defaultBackgroundColor = ConsoleColor.Black;
        private void ResetConsoleColor()
        {
            Console.ForegroundColor = defaultForegroundColor;
            Console.BackgroundColor = defaultBackgroundColor;
        }
        private void ShowSettingWindow()
        {
            try
            {
                if (windowthread == null)
                {
                    windowthread = new Thread(new ThreadStart(() =>
                    {
                        try
                        {
                            WriteLine("正在加载WPF库");
                            while (true)
                            {
                                try
                                {
                                    windowOpened = true;
                                    new MainWindow().ShowDialog();
                                    GC.Collect();
                                    windowOpened = false;
                                    manualResetEvent = new ManualResetEvent(false);
#if DEBUG
                                    WriteLine("窗体线程manualResetEvent返回:" +
#endif
                                    manualResetEvent.WaitOne()
#if DEBUG
                                    )
#endif
                                    ;
                                    manualResetEvent.Reset();
                                }
                                catch (Exception err) { WriteLine("窗体执行过程中发生错误\n信息" + err.ToString()); }
                            }
                        }
                        catch (Exception err) { WriteLine("窗体线程发生严重错误\n信息" + err.ToString()); windowthread = null; }
                    }));
                    windowthread.SetApartmentState(ApartmentState.STA);
                    windowthread.Start();
                }
                else
                { if (windowOpened) WriteLine("窗体已经打开"); else manualResetEvent.Set(); }
            }
            catch (Exception
#if DEBUG
            err
#endif
            )
            {
#if DEBUG
                WriteLine(err.ToString());
#endif
            }
        }

        /// <summary>
        /// 打开窗体按钮被按下
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件参数</param>
        public void MenuCall(object sender, CQMenuCallEventArgs e)
        {
            defaultForegroundColor = Console.ForegroundColor;
            defaultBackgroundColor = Console.BackgroundColor;
            //Console.WriteLine("TEST");
            ShowSettingWindow();
            //if (this._mainWindow == null)
            //{
            //    this._mainWindow = new MainWindow();
            //    this._mainWindow.Closing += MainWindow_Closing;
            //    this._mainWindow.Show();	// 显示窗体
            //}
            //else
            //{
            //    this._mainWindow.Activate();	// 将窗体调制到前台激活
            //}
        }

        //private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        //{
        //    // 对变量置 null, 因为被关闭的窗口无法重复显示
        //    this._mainWindow = null;
        //}
    }
}
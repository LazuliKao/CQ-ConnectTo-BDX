using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;

namespace ml.paradis.tool.UI
{
    public class Menu_OpenWindow : IMenuCall
    {
        private MainWindow _mainWindow = null;

        /// <summary>
        /// 打开窗体按钮被按下
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件参数</param>
        public void MenuCall(object sender, CQMenuCallEventArgs e)
        {
            if (this._mainWindow == null)
            {
                this._mainWindow = new MainWindow();
                this._mainWindow.Closing += MainWindow_Closing;
                this._mainWindow.Show();	// 显示窗体
            }
            else
            {
                this._mainWindow.Activate();	// 将窗体调制到前台激活
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 对变量置 null, 因为被关闭的窗口无法重复显示
            this._mainWindow = null;
        }
    }
}
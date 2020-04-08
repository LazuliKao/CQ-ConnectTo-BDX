using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;
using WebSocketSharp;

namespace ml.paradis.tool.Code
{
    public class Event_AppEnable : IAppEnable
    {
        /// <summary>
        /// APP启用
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件参数</param>
        public void AppEnable(object sender, CQAppEnableEventArgs e)
        {
            try
            {
                Data.E = e;
                Operation.Setup();
                Operation.AddLog("插件启动成功!\nCQ插件作者:gxh\nSDK:Native.Sdk\n编写语言C#\n需配合BDX端的Websocket插件使用!");
                Operation.AddLog($"当前{Data.WSClients.Count}个Websocket实例");
                for (int i = 0; i < Data.WSClients.Count; i++)
                {
                    Operation.AddLog($"编号{i+1}状态:{(Data.WSClients.Keys.ElementAt(i).IsAlive ? "已连接" : "离线")}");
                }
            }
            catch (Exception err)
            {
                Operation.AddLog("启动失败请重试!!!\n" + err.ToString());
            }
        }
    }
    public class Event_AppDisable : IAppDisable
    {
        public void AppDisable(object sender, CQAppDisableEventArgs e)
        {
            Operation.Quit();
        }
    }
}

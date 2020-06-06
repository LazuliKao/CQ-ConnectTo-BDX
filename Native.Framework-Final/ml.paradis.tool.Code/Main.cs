using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebSocketSharp;

namespace ml.paradis.tool.Code
{
    class Main
    {
        public static void Setup()
        {
            #region WebSocket实例初始化
            foreach (var server in Data.Config["Servers"])
            {
                Data.WSClients.Add(new WebSocket(server["Address"].ToString()), (JObject)server);
                Data.WSClients.Last().Key.OnMessage += (sender, e) =>
                {
                    Operation.AddLog((sender as WebSocket).Url + "Received:" + e.Data);
                    MessageCallback.WSCReceiveMessage(sender as WebSocket, e.Data);
                };
                Data.WSClients.Last().Key.OnClose += (sender, e) =>
                {
                    Operation.AddLog((sender as WebSocket).Url + "连接已断开:" + e.Code + "\n" + e.Reason);
                };
                Data.WSClients.Last().Key.OnError += (sender, e) =>
                {
                    Operation.AddLog((sender as WebSocket).Url + "运行出错:" + e.Exception + "\n" + e.Message);
                };
                Data.WSClients.Last().Key.OnOpen += (sender, e) =>
                {
                    Operation.AddLog((sender as WebSocket).Url + "连接已建立");
                    ((WebSocket)sender).Send($"{{\"operate\":\"setdesp\",\"desp\":\"BotByGxh(Ver:{Data.E.CQApi.AppInfo.Version}|logonQQ:{Data.E.CQApi.GetLoginQQId()})\"}}");
                };
                try
                {
                    Data.WSClients.Last().Key.Connect();
                    if (Data.WSClients.Last().Key.IsAlive)
                    {
                        Operation.AddLog($"{server["Tag"]}{Data.WSClients.Last().Key.Url}连接成功！");
                    }
                    else
                    {
                        Operation.AddLog($"{server["Tag"]}{Data.WSClients.Last().Key.Url}连接失败！");
                    }
                }
                catch (Exception err) { Operation.AddLog($"{server["Tag"]}{Data.WSClients.Last().Key.Url}连接失败！\n{err}"); }
            }
            #endregion
            #region 掉线检查初始化
            Data.KeepAliveTimer.Interval = double.Parse(Data.Config["CheckConnectionTime"].ToString()) * 1000;
            Data.KeepAliveTimer.Elapsed += (sender, e) =>
            {
                foreach (WebSocket ws in Data.WSClients.Keys.Where(l => !l.IsAlive))
                {
                    try
                    {
                        ws.Connect();
                        if (ws.Ping() && ws.IsAlive)
                        { Operation.AddLog($"[KeepAlive]实例重连成功！\n" + ws.Url); }
                        else
                        { Operation.AddLog($"[KeepAlive]实例重连失败！\n" + ws.Url); }
                    }
                    catch (Exception err) { Operation.AddLog($"重连失败！{ws.Url}\n{err}"); }
                }
            };
            Data.KeepAliveTimer.Start();
            #endregion
            #region Timers初始化
            if (Data.Config.ContainsKey("Timers"))
            {
                foreach (JObject Atimer in Data.Config["Timers"])
                {
                    System.Timers.Timer timer = new System.Timers.Timer(double.Parse(Atimer["Interval"].ToString()) * 1000);
                    timer.Elapsed += (sender, e) =>
                    {
                        try
                        {
                            MessageCallback.TimerOrTaskElapsedMessage(Data.Timers[sender as System.Timers.Timer]);
                            Data.E.CQLog.Debug("计时器触发", "#" + (Data.Timers.Keys.ToList().IndexOf(sender as System.Timers.Timer) + 1));
                        }
                        catch (Exception err) { Data.E.CQLog.Warning("计时器触发出错", err.Message + "\n" + Data.Timers[sender as System.Timers.Timer]); }
                    };
                    Data.Timers.Add(timer, Atimer);
                    Data.Timers.Keys.Last().Start();
                }
                int i = 0;
                Operation.AddLog($"--------初始化--------\n成功创建 {Data.Timers.Count()} 个计时器(Timers)\n#" + string.Join("秒\n#", Data.Timers.Values.ToList().ConvertAll(l => ++i + ">" + l["Interval"].ToString())) + "秒\n----------------------");
            }
            #endregion
            #region Tasks初始化
            if (Data.Config.ContainsKey("Tasks"))
            {
                int i = 0;
                Operation.AddLog($"--------初始化--------\n读取到 {Data.Config["Tasks"].Count()} 个定时任务(Tasks)\n#" + string.Join("\n#", Data.Config["Tasks"].ToList().ConvertAll(l => ++i + ">" + l["Mode"].ToString() + ":" + l["Time"].ToString())) + "\n----------------------");
                Operation.SetNextTask();
            }
            #endregion
        }
        public static void Restart()
        {
            Quit();
        }
        public static void Quit()
        {
            Data.KeepAliveTimer.Stop();
            foreach (var item in Data.WSClients.Keys)
            {
                item.Close();
            }
        }
    }
    class MessageCallback
    {
        #region WSC receive
        public static void WSCReceiveMessage(WebSocket wsc, string receiveData)
        {

            try
            {
                JObject receive = JObject.Parse(receiveData);
                JObject server = Data.WSClients[wsc];
                Dictionary<string, string> Variants = new Dictionary<string, string>();
                void DoActions(JArray actions)
                {
                    foreach (JObject action in actions)
                    {
                        try
                        {
                            if (Operation.ActionOperation(action, receive, ref Variants, server))
                            {
                                #region 特定操作
                                if (!action.ContainsKey("Type")) { throw new Exception("参数缺失:\"Type\""); }
                                if (!action.ContainsKey("Parameters")) { throw new Exception("参数缺失:\"Parameters\""); }
                                JObject Part = action["Parameters"] as JObject;
                                if (action.ContainsKey("Filter"))
                                {
                                    if (!Operation.CalculateExpressions(action["Filter"], receive, Variants))
                                    {
                                        #region 不满足条件的Actions
                                        switch (action["Type"].ToString().ToLower())
                                        {
                                            case "doactions":
                                            case "doaction":
                                            case "subactions":
                                            case "subaction":
                                                try
                                                {
                                                    if (Part.ContainsKey("MismatchedActions"))
                                                    {
                                                        DoActions((JArray)Part["MismatchedActions"]);
                                                    }
                                                }
                                                catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                                continue;
                                            default:
                                                continue;
                                        }
                                        #endregion
                                    }
                                }
                                switch (action["Type"].ToString().ToLower())
                                {
                                    case "doactions":
                                    case "doaction":
                                    case "subactions":
                                    case "subaction":
                                        try
                                        {
                                            DoActions((JArray)Part["Actions"]);
                                        }
                                        catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        continue;
                                    case "sender":
                                        try
                                        {
                                            wsc.Send(Data.GetCmdReq(server["Passwd"].ToString(), Operation.Format(Part["cmd"].ToString(), Variants)));
                                        }
                                        catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        break;
                                    case "other":
                                        foreach (WebSocket ws in Data.WSClients.Keys.Where(l => l != wsc && l.IsAlive))
                                        {
                                            try
                                            { ws.Send(Data.GetCmdReq(server["Passwd"].ToString(), Operation.Format(Part["cmd"].ToString(), Variants))); }
                                            catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        }
                                        break;
                                    case "all":
                                        foreach (WebSocket ws in Data.WSClients.Keys.Where(l => l.IsAlive))
                                        {
                                            try
                                            { ws.Send(Data.GetCmdReq(server["Passwd"].ToString(), Operation.Format(Part["cmd"].ToString(), Variants))); }
                                            catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        }
                                        break;
                                    default:
                                        Data.E.CQLog.Error("[WS接收]ERROR[Actions]", $"未知操作{action["Type"]}\n位于{action}");
                                        continue;
                                }
                                #endregion
                            }
                        }
                        catch (Exception err)
                        {
                            Data.E.CQLog.Error("[WS接收]ERROR", err.Message);
                        }
                    }
                }
                DoActions((JArray)server["Actions"]);
                #region CMDcallback 
                try
                {
                    Data.CallBackInfo get = Data.CMDQueue.First(new Func<Data.CallBackInfo, bool>(l => l.uuid == receive["msgid"].ToString()));
                    Data.CMDQueue.Remove(get);
                    Variants = get.Variants;
                    DoActions(get.CallbackActions);
                }
                catch (Exception) { }
                #endregion
            }
            catch (Exception err)
            { Operation.AddLog(err.ToString()); }
        }
        #endregion
        #region GM receive
        public static void GroupReceiveMessage(ref Native.Sdk.Cqp.EventArgs.CQGroupMessageEventArgs ev, JObject group)
        {
            try
            {
                var MemberInfo = ev.FromQQ.GetGroupMemberInfo(ev.FromGroup);
                var GroupInfo = ev.FromGroup.GetGroupInfo();
                JObject receive = new JObject() {
                            new JProperty("Message", Native.Sdk.Cqp.CQApi.CQDeCode(ev.Message.Text)/*.Replace("&#91;", "[").Replace("&#93;", "]")*/),
                            new JProperty("FromQQ", ev.FromQQ.Id),
                            new JProperty("FromQQNick",MemberInfo.Nick),
                            new JProperty("FromGroup", ev.FromGroup.Id),
                            new JProperty("IsFromAnonymous", ev.IsFromAnonymous),
                            new JProperty("Id", ev.Id),
                            new JProperty("MemberInfo",
                                                                                new JObject(){
                                                                                    new JProperty("Card",MemberInfo. Card),
                                                                                    new JProperty("Sex",MemberInfo. Sex),
                                                                                    new JProperty("Age",MemberInfo. Age),
                                                                                    new JProperty("Area",MemberInfo. Area),
                                                                                    new JProperty("JoinGroupDateTime",MemberInfo. JoinGroupDateTime),
                                                                                    new JProperty("LastSpeakDateTime",MemberInfo. LastSpeakDateTime),
                                                                                    new JProperty("Level",MemberInfo. Level),
                                                                                    new JProperty("MemberType",MemberInfo. MemberType.ToString()),
                                                                                    new JProperty("ExclusiveTitle",MemberInfo. ExclusiveTitle),
                                                                                    new JProperty("ExclusiveTitleExpirationTime",MemberInfo. ExclusiveTitleExpirationTime)
                                                                                                    }
                            ),
                            new  JProperty("GroupInfo",
                                                                                new JObject(){
                                                                                    new JProperty("Name", GroupInfo.Name),
                                                                                    new JProperty("CurrentMemberCount", GroupInfo.CurrentMemberCount),
                                                                                    new JProperty("MaxMemberCount", GroupInfo.MaxMemberCount)
                                                                                }
                            )
                        };
                Dictionary<string, string> Variants = new Dictionary<string, string>();
                void DoActions(JArray actions, ref Native.Sdk.Cqp.EventArgs.CQGroupMessageEventArgs e)
                {
                    foreach (JObject action in actions)
                    {
                        try
                        {
                            if (Operation.ActionOperation(action, receive, ref Variants, group))
                            {
                                #region 特定操作
                                if (!action.ContainsKey("Type")) { throw new Exception("参数缺失:\"Type\""); }
                                if (!action.ContainsKey("Parameters")) { throw new Exception("参数缺失:\"Parameters\""); }
                                JObject Part = action["Parameters"] as JObject;
                                if (action.ContainsKey("Filter"))
                                {
                                    if (!Operation.CalculateExpressions(action["Filter"], receive, Variants))
                                    {
                                        #region 不满足条件的Actions
                                        switch (action["Type"].ToString().ToLower())
                                        {
                                            case "doactions":
                                            case "doaction":
                                            case "subactions":
                                            case "subaction":
                                                try
                                                {
                                                    if (Part.ContainsKey("MismatchedActions"))
                                                    {
                                                        DoActions((JArray)Part["MismatchedActions"], ref e);
                                                    }
                                                }
                                                catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                                continue;
                                            default:
                                                continue;
                                        }
                                        #endregion
                                    }
                                }
                                switch (action["Type"].ToString().ToLower())
                                {
                                    case "doactions":
                                    case "doaction":
                                    case "subactions":
                                    case "subaction":
                                        try
                                        {
                                            DoActions((JArray)Part["Actions"], ref e);
                                        }
                                        catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        continue;
                                    case "returngroupmessageatfrom":
                                        try
                                        {
                                            Event_GroupMessage.ReturnGMAtFrom(ref e, Operation.Format(Part["Message"].ToString(), Variants));
                                        }
                                        catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        break;
                                    case "returnprivatemessage":
                                        try
                                        {
                                            if (Part.ContainsKey("QQ"))
                                            {
                                                e.CQApi.SendPrivateMessage(long.Parse(Operation.Format(Part["QQ"].ToString(), Variants)), Operation.Format(Part["Message"].ToString(), Variants));
                                            }
                                            else
                                            {
                                                Event_GroupMessage.ReturnPrivateMessage(ref e, Operation.Format(Part["Message"].ToString(), Variants));
                                            }
                                        }
                                        catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        break;
                                    default:
                                        Data.E.CQLog.Error("[GM接收]ERROR[Actions]", $"未知操作{action["Type"]}\n位于{action}");
                                        continue;
                                }
                                #endregion
                            }
                        }
                        catch (Exception err)
                        {
                            Data.E.CQLog.Error("[GM接收]ERROR", err.Message);
                        }
                    }
                }
                DoActions((JArray)group["Actions"], ref ev);
            }
            catch (Exception err)
            { Operation.AddLog(err.ToString()); }
        }
        #endregion
        #region Timer Elapsed&Task
        public static void TimerOrTaskElapsedMessage(JObject timer)
        {
            try
            {
                JObject receive = new JObject() { };
                Dictionary<string, string> Variants = new Dictionary<string, string>();
                void DoActions(JArray actions)
                {
                    foreach (JObject action in actions)
                    {
                        try
                        {
                            if (Operation.ActionOperation(action, receive, ref Variants, timer))
                            {
                                #region 特定操作
                                if (!action.ContainsKey("Type")) { throw new Exception("参数缺失:\"Type\""); }
                                if (!action.ContainsKey("Parameters")) { throw new Exception("参数缺失:\"Parameters\""); }
                                JObject Part = action["Parameters"] as JObject;
                                if (action.ContainsKey("Filter"))
                                {
                                    if (!Operation.CalculateExpressions(action["Filter"], receive, Variants))
                                    {
                                        #region 不满足条件的Actions
                                        switch (action["Type"].ToString().ToLower())
                                        {
                                            case "doactions":
                                            case "doaction":
                                            case "subactions":
                                            case "subaction":
                                                try
                                                {
                                                    if (Part.ContainsKey("MismatchedActions"))
                                                    {
                                                        DoActions((JArray)Part["MismatchedActions"]);
                                                    }
                                                }
                                                catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                                continue;
                                            default:
                                                continue;
                                        }
                                        #endregion
                                    }
                                }
                                switch (action["Type"].ToString().ToLower())
                                {
                                    case "doactions":
                                    case "doaction":
                                    case "subactions":
                                    case "subaction":
                                        try
                                        {
                                            DoActions((JArray)Part["Actions"]);
                                        }
                                        catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        continue;

                                    case "qqgroup":
                                    case "group":
                                    case "groupmessage":
                                    case "gm":
                                    case "发送群消息":
                                        try
                                        {
                                            Data.E.CQApi.SendGroupMessage(long.Parse(Operation.Format(Part["GroupID"].ToString(), Variants)), Operation.Format(Part["Message"].ToString(), Variants));
                                        }
                                        catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        break;
                                    default:
                                        Data.E.CQLog.Error("[TT触发]ERROR[Actions]", $"未知操作{action["Type"]}\n位于{action}");
                                        continue;
                                }
                                #endregion
                            }
                        }
                        catch (Exception err)
                        {
                            Data.E.CQLog.Error("[TT触发]ERROR", err.Message);
                        }
                    }
                }
                DoActions((JArray)timer["Actions"]);
            }
            catch (Exception err)
            { Operation.AddLog(err.ToString()); }
        }
        #endregion
    }
}

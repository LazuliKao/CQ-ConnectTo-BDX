using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                            foreach (JObject action in Data.Timers[sender as System.Timers.Timer]["Actions"])
                            {
                                #region 执行Actions
                                Dictionary<string, string> Variants = new Dictionary<string, string>();
                                switch (action["Target"].ToString())
                                {
                                    case "log":
                                        try
                                        {
                                            if (action.ContainsKey("Info"))
                                                _ = Data.E.CQLog.Info("CQToBDX-Log", Operation.Format(action["Info"].ToString(), Variants));
                                            if (action.ContainsKey("Debug"))
                                                _ = Data.E.CQLog.Debug("CQToBDX-Log", Operation.Format(action["Debug"].ToString(), Variants));
                                            if (action.ContainsKey("Warning"))
                                                _ = Data.E.CQLog.Warning("CQToBDX-Log", Operation.Format(action["Warning"].ToString(), Variants));
                                            if (action.ContainsKey("Fatal"))
                                                _ = Data.E.CQLog.Fatal("CQToBDX-Log", Operation.Format(action["Fatal"].ToString(), Variants));
                                        }
                                        catch (Exception) { }
                                        break;
                                    case "servers":
                                        if (action.ContainsKey("cmd"))
                                        {
                                            if (action.ContainsKey("Filter"))
                                            {
                                                var lambda_receive = new JObject();
                                                var lambda_Variants = Variants;
                                                foreach (var ws in Data.WSClients.Where(l => l.Key.IsAlive && Operation.CalculateExpressions(action["Filter"], lambda_receive, lambda_Variants, l.Value)))
                                                {
                                                    ws.Key.Send(Data.GetCmdReq(ws.Value["Passwd"].ToString(), Operation.Format(action["cmd"].ToString(), Variants)));
                                                }
                                            }
                                            else
                                            {
                                                foreach (var ws in Data.WSClients.Where(l => l.Key.IsAlive))
                                                {
                                                    ws.Key.Send(Data.GetCmdReq(ws.Value["Passwd"].ToString(), Operation.Format(action["cmd"].ToString(), Variants)));
                                                }
                                            }
                                        }
                                        break;
                                    case "QQGroup":
                                        if (action.ContainsKey("GroupID"))
                                        {
                                            Data.E.CQApi.SendGroupMessage(long.Parse(action["GroupID"].ToString()), Operation.Format(action["Message"].ToString(), Variants));
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                #endregion
                            }
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
                                { if (!Operation.CalculateExpressions(action["Filter"], receive, Variants)) { continue; } }
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
                                    case "qqgroup":
                                    case "group":
                                    case "groupmessage":
                                    case "gm":
                                    case "发送群消息":
                                        try
                                        { Data.E.CQApi.SendGroupMessage(long.Parse(Part["GroupID"].ToString()), Operation.Format(Part["Message"].ToString(), Variants)); }
                                        catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        break;
                                    default:
                                        Data.E.CQLog.Error("[WS接收]ERROR[Actions]", $"未知操作{action["Type"].ToString()}\n位于{action}");
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

                //    #region 1
                //    //JObject server = Data.WSClients[wsc];
                //    void DoTriggers(JArray triggers)
                //    {
                //        foreach (JObject trigger in triggers)
                //        {
                //            Operation.VariantCreate(trigger, receive, out Dictionary<string, string> Variants, server);
                //            Operation.VariantOperation(trigger, receive, ref Variants);
                //            //#region 过滤条件计算
                //            //if (trigger.ContainsKey("Filter"))
                //            //{
                //            //    void DoActions(JObject action)
                //            //    {
                //            //        switch (action["Target"].ToString())
                //            //        {
                //            //            case "log":
                //            //                try
                //            //                {
                //            //                    if (action.ContainsKey("Info"))
                //            //                        _ = Data.E.CQLog.Info("CQToBDX-Log", Operation.Format(action["Info"].ToString(), Variants));
                //            //                    if (action.ContainsKey("Debug"))
                //            //                        _ = Data.E.CQLog.Debug("CQToBDX-Log", Operation.Format(action["Debug"].ToString(), Variants));
                //            //                    if (action.ContainsKey("Warning"))
                //            //                        _ = Data.E.CQLog.Warning("CQToBDX-Log", Operation.Format(action["Warning"].ToString(), Variants));
                //            //                    if (action.ContainsKey("Fatal"))
                //            //                        _ = Data.E.CQLog.Fatal("CQToBDX-Log", Operation.Format(action["Fatal"].ToString(), Variants));
                //            //                }
                //            //                catch (Exception) { }
                //            //                break;
                //            //            case "sender":
                //            //                if (action.ContainsKey("cmd"))
                //            //                {
                //            //                    wsc.Send(Data.GetCmdReq(server["Passwd"].ToString(), Operation.Format(action["cmd"].ToString(), Variants)));
                //            //                }
                //            //                break;
                //            //            case "other":
                //            //                foreach (WebSocket ws in Data.WSClients.Keys.Where(l => l != wsc && l.IsAlive))
                //            //                {
                //            //                    if (action.ContainsKey("cmd"))
                //            //                    { ws.Send(Data.GetCmdReq(server["Passwd"].ToString(), Operation.Format(action["cmd"].ToString(), Variants))); }
                //            //                }
                //            //                break;
                //            //            case "all":
                //            //                foreach (WebSocket ws in Data.WSClients.Keys.Where(l => l.IsAlive))
                //            //                {
                //            //                    if (action.ContainsKey("cmd"))
                //            //                    { ws.Send(Data.GetCmdReq(server["Passwd"].ToString(), Operation.Format(action["cmd"].ToString(), Variants))); }
                //            //                }
                //            //                break;
                //            //            case "QQGroup":
                //            //                if (action.ContainsKey("GroupID"))
                //            //                {
                //            //                    Data.E.CQApi.SendGroupMessage(long.Parse(action["GroupID"].ToString()), Operation.Format(action["Message"].ToString(), Variants));
                //            //                }
                //            //                break;
                //            //            case "doTriggers":
                //            //                DoTriggers((JArray)action["Triggers"]);
                //            //                break;
                //            //            default:
                //            //                break;
                //            //        }
                //            //    }
                //            //    if (Operation.CalculateExpressions(trigger["Filter"], receive, Variants))
                //            //    {
                //            //        #region 满足条件Actions 
                //            //        if (trigger.ContainsKey("Actions"))
                //            //        {
                //            //            foreach (JObject action in trigger["Actions"])
                //            //            {
                //            //                DoActions(action);
                //            //            }
                //            //        }
                //            //        #endregion
                //            //    }
                //            //    else
                //            //    {
                //            //        #region 不满足条件Actions 
                //            //        if (trigger.ContainsKey("MismatchedActions"))
                //            //        {
                //            //            foreach (JObject action in trigger["MismatchedActions"])
                //            //            {
                //            //                DoActions(action);
                //            //            }
                //            //        }
                //            //        #endregion
                //            //    }
                //            //}
                //            //#endregion
                //        }
                //    }
                //    DoTriggers((JArray)server["Triggers"]);
                //    #endregion
                //
            }
            catch (Exception err)
            { Operation.AddLog(err.ToString()); }
        }
        #endregion
        #region WSC receive
        public static void GroupReceiveMessage(Native.Sdk.Cqp.EventArgs.CQGroupMessageEventArgs e, string receiveData)
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
                                { if (!Operation.CalculateExpressions(action["Filter"], receive, Variants)) { continue; } }
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
                                    case "qqgroup":
                                    case "group":
                                    case "groupmessage":
                                    case "gm":
                                    case "发送群消息":
                                        try
                                        { Data.E.CQApi.SendGroupMessage(long.Parse(Part["GroupID"].ToString()), Operation.Format(Part["Message"].ToString(), Variants)); }
                                        catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                                        break;
                                    default:
                                        Data.E.CQLog.Error("[WS接收]ERROR[Actions]", $"未知操作{action["Type"].ToString()}\n位于{action}");
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
            }
            catch (Exception err)
            { Operation.AddLog(err.ToString()); }
        }
        #endregion
    }
}

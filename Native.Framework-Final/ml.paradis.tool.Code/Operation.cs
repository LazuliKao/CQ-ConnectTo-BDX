﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace ml.paradis.tool.Code
{
    class Operation
    {
        #region 获取最近的Task
        private static KeyValuePair<double, JObject> GetNextTask()
        {
            int index = -1;
            double interval = double.MaxValue;
            for (int i = 0; i < Data.Config["Tasks"].Count(); i++)
            {
                double timeI = 0;
                DateTime TimeNow = DateTime.Now;
                switch (Data.Config["Tasks"][i]["Mode"].ToString())
                {
                    case "EachDay":
                        timeI = (3600000 * double.Parse(Data.Config["Tasks"][i]["Time"].ToString())) - TimeNow.TimeOfDay.TotalMilliseconds;
                        timeI = timeI > 0 ? timeI : 86400000 + timeI;
                        if (timeI < interval) { interval = timeI; index = i; }
                        break;
                    case "EachHour":
                        timeI = (60000 * double.Parse(Data.Config["Tasks"][i]["Time"].ToString())) - (TimeNow.TimeOfDay.TotalMilliseconds % 3600000);
                        //   AddLog("a"+(timeI/1000/60).ToString());
                        timeI = timeI > 0 ? timeI : 3600000 + timeI;
                        //   AddLog("b"+(timeI / 1000 / 60).ToString());
                        if (timeI < interval) { interval = timeI; index = i; }
                        break;
                    default:
                        break;
                }
            }
            return new KeyValuePair<double, JObject>(interval, Data.Config["Tasks"][index] as JObject);
        }
        public static void SetNextTask()
        {
            KeyValuePair<double, JObject> NextTask = GetNextTask();
            Data.TaskTimer.Dispose();
            Data.TaskTimer = new System.Timers.Timer() { AutoReset = false };
            Data.TaskTimer.Interval = NextTask.Key;
            Data.TaskTimerActions = NextTask.Value;
            Data.TaskTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    foreach (JObject action in Data.TaskTimerActions["Actions"])
                    {
                        #region 执行Actions
                        Dictionary<string, string> Variants = new Dictionary<string, string>();
                        switch (action["Target"].ToString())
                        {
                            case "log":
                                try
                                {
                                    if (action.ContainsKey("Info"))
                                        _ = Data.E.CQLog.Info("CQToBDX-Log", Format(action["Info"].ToString(), Variants));
                                    if (action.ContainsKey("Debug"))
                                        _ = Data.E.CQLog.Debug("CQToBDX-Log", Format(action["Debug"].ToString(), Variants));
                                    if (action.ContainsKey("Warning"))
                                        _ = Data.E.CQLog.Warning("CQToBDX-Log", Format(action["Warning"].ToString(), Variants));
                                    if (action.ContainsKey("Fatal"))
                                        _ = Data.E.CQLog.Fatal("CQToBDX-Log", Format(action["Fatal"].ToString(), Variants));
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
                    Data.E.CQLog.Debug("定时任务触发", Data.TaskTimerActions.ToString());
                }
                catch (Exception err) { Data.E.CQLog.Warning("定时任务触发出错", err.Message + "\n" + Data.Timers[sender as System.Timers.Timer]); }
                SetNextTask();
            };
            Data.TaskTimer.Start();

            Data.E.CQLog.Debug("定时任务", $"当前时间:\t{DateTime.Now:HH:mm:ss}\n下一任务:\t{DateTime.Now.AddMilliseconds(NextTask.Key):HH:mm:ss}\n距离下一个定时任务触发还有{(NextTask.Key < 3600000 ? null : TimeSpan.FromMilliseconds(NextTask.Key).Hours + "小时")}{TimeSpan.FromMilliseconds(NextTask.Key).Minutes}分{TimeSpan.FromMilliseconds(NextTask.Key).Seconds}秒");
        }
        #endregion

        #region 计算值  

        #region 1.变量创建
        public static void VariantCreate(JObject trigger, JObject receive, out Dictionary<string, string> Variants, JObject server)
        {
            Variants = new Dictionary<string, string>();
            if (trigger.ContainsKey("Variants"))
            {
                foreach (JObject Variant in trigger["Variants"])
                {
                    try
                    {
                        if (Variant.ContainsKey("Path"))
                        {
                            Variants.Add(Variant["Name"].ToString(), GetSubItem(receive, Variant["Path"].ToList().ConvertAll(l => l.ToString())));
                        }
                        else
                        {
                            Variants.Add(Variant["Name"].ToString(), GetSubItem(server, Variant["ServerConfig"].ToList().ConvertAll(l => l.ToString())));
                        }
                    }
                    catch (Exception err) { AddLog("貌似有个变量加载失败!" + err.ToString()); }
                }
            }
        }
        #endregion

        #region 2.计算操作处理
        public static void VariantOperation(JObject trigger, JObject receive, ref Dictionary<string, string> Variants)
        {
            if (trigger.ContainsKey("Operations"))
            {
                foreach (JObject operation in trigger["Operations"])
                {
                    try
                    {
                        if (operation.ContainsKey("Type"))
                        {
                            if (operation.ContainsKey("Filter"))
                            { if (!CalculateExpressions(operation["Filter"], receive, Variants)) { continue; } }
                            switch (operation["Type"].ToString())
                            {
                                case "Replace":
                                    try
                                    {
                                        if (operation.ContainsKey("CreateVariant"))
                                        {
                                            Variants.Add(operation["CreateVariant"].ToString(), Variants[operation["TargetVariant"].ToString()].Replace(operation["Find"].ToString(), operation["Replacement"].ToString()));
                                        }
                                        else
                                        {
                                            string TargetVariant = operation["TargetVariant"].ToString();
                                            Variants[TargetVariant] = Variants[TargetVariant].Replace(operation["Find"].ToString(), operation["Replacement"].ToString());
                                        }
                                    }
                                    catch (Exception err)
                                    { AddLog($"参数缺失或变量不存在\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                                    break;
                                case "RegexReplace":
                                    try
                                    {
                                        if (operation.ContainsKey("CreateVariant"))
                                        {
                                            Variants.Add(operation["CreateVariant"].ToString(), Regex.Replace(Variants[operation["TargetVariant"].ToString()], operation["Pattern"].ToString(), operation["Replacement"].ToString()));
                                        }
                                        else
                                        {
                                            string TargetVariant = operation["TargetVariant"].ToString();
                                            Variants[TargetVariant] = Regex.Replace(Variants[TargetVariant], operation["Pattern"].ToString(), operation["Replacement"].ToString());
                                        }
                                    }
                                    catch (Exception err)
                                    { AddLog($"可能是参数缺失或变量或正则表达式有误不存在\nVarList:{string.Join("\t", Variants)}\n位于{operation}\n错误内容{err.Message}"); }
                                    break;
                                case "RegexGet":
                                    try
                                    {
                                        if (operation.ContainsKey("CreateVariant"))
                                        {
                                            Variants.Add(operation["CreateVariant"].ToString(), Regex.Match(Variants[operation["TargetVariant"].ToString()], operation["Pattern"].ToString()).Groups[operation["GroupName"].ToString()].Value);
                                        }
                                        else
                                        {
                                            string TargetVariant = operation["TargetVariant"].ToString();
                                            Variants[TargetVariant] = Regex.Match(Variants[TargetVariant], operation["Pattern"].ToString()).Groups[operation["GroupName"].ToString()].Value;
                                        }
                                    }
                                    catch (Exception err)
                                    { AddLog($"可能是参数缺失或变量或正则表达式有误不存在\nVarList:{string.Join("\t", Variants)}\n位于{operation}\n错误内容{err.Message}"); }
                                    break;
                                case "Format":
                                    try
                                    {
                                        if (operation.ContainsKey("CreateVariant"))
                                        {
                                            Variants.Add(operation["CreateVariant"].ToString(), Format(operation["Text"].ToString(), Variants));
                                        }
                                        else
                                        {
                                            string TargetVariant = operation["TargetVariant"].ToString();
                                            Variants[TargetVariant] = Format(operation["Text"].ToString(), Variants);
                                        }
                                    }
                                    catch (Exception err)
                                    { AddLog($"Format参数缺失或变量或格式有误不存在\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                                    break;
                                case "ToUnicode":
                                    try
                                    {
                                        if (operation.ContainsKey("CreateVariant"))
                                        {
                                            Variants.Add(operation["CreateVariant"].ToString(), Operation.StringToUnicode(Format(operation["Text"].ToString(), Variants)));
                                        }
                                        else
                                        {
                                            if (operation.ContainsKey("TargetVariant"))
                                            {
                                                string TargetVariant = operation["TargetVariant"].ToString();
                                                Variants[TargetVariant] = Operation.StringToUnicode(Variants[TargetVariant]);
                                            }
                                        }
                                    }
                                    catch (Exception err)
                                    { AddLog($"ToUnicode参数缺失或变量或格式有误不存在\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                                    break;
                                case "MotdBE":
                                    try
                                    {
                                        if (operation.ContainsKey("CreateVariant"))
                                        {
                                            try
                                            {
                                                Variants.Add(operation["CreateVariant"].ToString(),
                                                    Format(Format(operation["Text"].ToString(), GetServerInfoD(
                                                      Format(operation["IP"].ToString(), Variants),
                                                      Format(operation["PORT"].ToString(), Variants)),
                                                        "$"), Variants)
                                                    );
                                            }
                                            catch (Exception err)
                                            {
                                                Variants.Add(operation["CreateVariant"].ToString(), Format(string.Format(operation["FailedText"].ToString(), err.Message), Variants));
                                            }
                                        }
                                    }
                                    catch (Exception err)
                                    { AddLog($"MotdBE执行出错\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                                    break;
                                case "GetHTML":
                                    try
                                    {
                                        if (operation.ContainsKey("CreateVariant"))
                                        {
                                            try
                                            {
                                                Native.Tool.Http.HttpWebClient webClient = new Native.Tool.Http.HttpWebClient();
                                                Variants.Add(operation["CreateVariant"].ToString(),
                                                    Encoding.UTF8.GetString(webClient.DownloadData(Format(operation["URI"].ToString(), Variants)))
                                                    );
                                            }
                                            catch (Exception err)
                                            {
                                                Variants.Add(operation["CreateVariant"].ToString(), "获取失败" + err.Message);
                                            }
                                        }
                                    }
                                    catch (Exception err)
                                    { AddLog($"GetHTML执行出错\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                                    break;
                                case "Sleep":
                                    try
                                    {
                                        if (operation.ContainsKey("Time"))
                                        {
                                            Thread.Sleep(int.Parse(operation["Time"].ToString()));
                                        }
                                    }
                                    catch (Exception err)
                                    { AddLog($"Sleep执行出错\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                                    break;
                                //case "write_ini":
                                //    try
                                //    {
                                //        Native.Tool.IniConfig.Attribute. iniConfig = new Native.Tool.IniConfig();
                                //        if (operation.ContainsKey("Time"))
                                //        {
                                //            Thread.Sleep(int.Parse(operation["Time"].ToString()));
                                //        }
                                //    }
                                //    catch (Exception err)
                                //    { AddLog($"ini写入出错\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                                //    break;
                                //case "read_ini":
                                //    try
                                //    {
                                //        if (operation.ContainsKey("Time"))
                                //        {
                                //            Thread.Sleep(int.Parse(operation["Time"].ToString()));
                                //        }
                                //    }
                                //    catch (Exception err)
                                //    { AddLog($"ini读取出错\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                                //    break;
                                default:
                                    break;
                            }
                        }
                        else
                        { AddLog("未指明Type参数" + trigger.Path.ToString()); }
                    }
                    catch (Exception err) { AddLog("貌似有个操作执行失败!" + err.Message); }
                }
            }
        }
        public static bool ActionOperation(JObject action, JObject receive, ref Dictionary<string, string> Variants)
        {
            if (!action.ContainsKey("Type")) { throw new Exception("参数缺失:\"Type\""); }
            if (!action.ContainsKey("Parameters")) { throw new Exception("参数缺失:\"Parameters\""); }
            JObject Part = action["Parameters"] as JObject;
            switch (action["Type"].ToString().ToLower())
            {
                case "createvariant":
                    try
                    { 
                    }
                    catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;

                case "replace":
                    try
                    {
                        if (true)
                        {

                        }
                        //if (operation.ContainsKey("CreateVariant"))
                        //{
                        //    Variants.Add(operation["CreateVariant"].ToString(), Variants[operation["TargetVariant"].ToString()].Replace(operation["Find"].ToString(), operation["Replacement"].ToString()));
                        //}
                        //else
                        //{
                        //    string TargetVariant = operation["TargetVariant"].ToString();
                        //    Variants[TargetVariant] = Variants[TargetVariant].Replace(operation["Find"].ToString(), operation["Replacement"].ToString());
                        //}
                    }
                    catch (Exception err)      { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "regexreplace":
                    try
                    {
                        if (operation.ContainsKey("CreateVariant"))
                        {
                            Variants.Add(operation["CreateVariant"].ToString(), Regex.Replace(Variants[operation["TargetVariant"].ToString()], operation["Pattern"].ToString(), operation["Replacement"].ToString()));
                        }
                        else
                        {
                            string TargetVariant = operation["TargetVariant"].ToString();
                            Variants[TargetVariant] = Regex.Replace(Variants[TargetVariant], operation["Pattern"].ToString(), operation["Replacement"].ToString());
                        }
                    }
                    catch (Exception err)
                    { AddLog($"可能是参数缺失或变量或正则表达式有误不存在\nVarList:{string.Join("\t", Variants)}\n位于{operation}\n错误内容{err.Message}"); }
                    break;
                case "regexget":
                    try
                    {
                        if (operation.ContainsKey("CreateVariant"))
                        {
                            Variants.Add(operation["CreateVariant"].ToString(), Regex.Match(Variants[operation["TargetVariant"].ToString()], operation["Pattern"].ToString()).Groups[operation["GroupName"].ToString()].Value);
                        }
                        else
                        {
                            string TargetVariant = operation["TargetVariant"].ToString();
                            Variants[TargetVariant] = Regex.Match(Variants[TargetVariant], operation["Pattern"].ToString()).Groups[operation["GroupName"].ToString()].Value;
                        }
                    }
                    catch (Exception err)
                    { AddLog($"可能是参数缺失或变量或正则表达式有误不存在\nVarList:{string.Join("\t", Variants)}\n位于{operation}\n错误内容{err.Message}"); }
                    break;
                case "format":
                    try
                    {
                        if (operation.ContainsKey("CreateVariant"))
                        {
                            Variants.Add(operation["CreateVariant"].ToString(), Format(operation["Text"].ToString(), Variants));
                        }
                        else
                        {
                            string TargetVariant = operation["TargetVariant"].ToString();
                            Variants[TargetVariant] = Format(operation["Text"].ToString(), Variants);
                        }
                    }
                    catch (Exception err)
                    { AddLog($"Format参数缺失或变量或格式有误不存在\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                    break;
                case "tounicode":
                    try
                    {
                        if (operation.ContainsKey("CreateVariant"))
                        {
                            Variants.Add(operation["CreateVariant"].ToString(), Operation.StringToUnicode(Format(operation["Text"].ToString(), Variants)));
                        }
                        else
                        {
                            if (operation.ContainsKey("TargetVariant"))
                            {
                                string TargetVariant = operation["TargetVariant"].ToString();
                                Variants[TargetVariant] = Operation.StringToUnicode(Variants[TargetVariant]);
                            }
                        }
                    }
                    catch (Exception err)
                    { AddLog($"ToUnicode参数缺失或变量或格式有误不存在\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                    break;
                case "motdbe":
                    try
                    {
                        if (operation.ContainsKey("CreateVariant"))
                        {
                            try
                            {
                                Variants.Add(operation["CreateVariant"].ToString(),
                                    Format(Format(operation["Text"].ToString(), GetServerInfoD(
                                      Format(operation["IP"].ToString(), Variants),
                                      Format(operation["PORT"].ToString(), Variants)),
                                        "$"), Variants)
                                    );
                            }
                            catch (Exception err)
                            {
                                Variants.Add(operation["CreateVariant"].ToString(), Format(string.Format(operation["FailedText"].ToString(), err.Message), Variants));
                            }
                        }
                    }
                    catch (Exception err)
                    { AddLog($"MotdBE执行出错\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                    break;
                case "gethtml":
                    try
                    {
                        if (operation.ContainsKey("CreateVariant"))
                        {
                            try
                            {
                                Native.Tool.Http.HttpWebClient webClient = new Native.Tool.Http.HttpWebClient();
                                Variants.Add(operation["CreateVariant"].ToString(),
                                    Encoding.UTF8.GetString(webClient.DownloadData(Format(operation["URI"].ToString(), Variants)))
                                    );
                            }
                            catch (Exception err)
                            {
                                Variants.Add(operation["CreateVariant"].ToString(), "获取失败" + err.Message);
                            }
                        }
                    }
                    catch (Exception err)
                    { AddLog($"GetHTML执行出错\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                    break;
                case "sleep":
                    try
                    {
                        if (operation.ContainsKey("Time"))
                        {
                            Thread.Sleep(int.Parse(operation["Time"].ToString()));
                        }
                    }
                    catch (Exception err)
                    { AddLog($"Sleep执行出错\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                    break;
                //case "write_ini":
                //    try
                //    {
                //        Native.Tool.IniConfig.Attribute. iniConfig = new Native.Tool.IniConfig();
                //        if (operation.ContainsKey("Time"))
                //        {
                //            Thread.Sleep(int.Parse(operation["Time"].ToString()));
                //        }
                //    }
                //    catch (Exception err)
                //    { AddLog($"ini写入出错\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                //    break;
                //case "read_ini":
                //    try
                //    {
                //        if (operation.ContainsKey("Time"))
                //        {
                //            Thread.Sleep(int.Parse(operation["Time"].ToString()));
                //        }
                //    }
                //    catch (Exception err)
                //    { AddLog($"ini读取出错\nVarCount:{Variants.Count}\n位于{operation}\n错误内容{err.Message}"); }
                //    break;
                default:
                    return false;
            }
            return true;
        }
        #endregion

        #region 3.字符串格式化
        #region format
        public static string Format(string input, Dictionary<string, string> vars)
        {
            if (string.IsNullOrEmpty(input)) { return ""; }
            string processText = input;
            while (true)
            {
                Match match = Regex.Match(processText, @"%(\w+?)%");
                if (match.Success)
                {
                    string searched = "null";
                    try { searched = vars[match.Groups[1].Value]; }
                    catch (Exception) { }
                    processText = processText.Replace(match.Value, searched);
                }
                else { break; }
            }
            return processText;
        }
        public static string Format(string input, Dictionary<string, string> vars, string tag)
        {
            if (string.IsNullOrEmpty(input)) { return ""; }
            string processText = input;
            while (true)
            {
                Match match = Regex.Match(processText, $@"{tag}(\w+?){tag}");
                if (match.Success)
                {
                    string searched = "null";
                    try { searched = vars[match.Groups[1].Value]; }
                    catch (Exception) { }
                    processText = processText.Replace(match.Value, searched);
                }
                else { break; }
            }
            return processText;
        }
        #endregion

        #endregion
        #region 服务器Motd
        private enum InfoList
        {
            type, description, connectionVer, gameVer, onlineplayers, maxPlayers,
            serverUID, mapName, defaultMode, isBDS, port, portv6
            //,
            //类别 = 0, 简介, 协议版本, 游戏版本, 在线人数, 在线在线人数,
            //客户端标识, 存档名称, 默认模式, _未知2, 端口, ipv6端口
        }
        private static Dictionary<InfoList, string> GetServerInfo(string address, int port)
        {
            byte[] sendData = Convert.FromBase64String("AQAAAAAAA2oHAP//AP7+/v79/f39EjRWeJx0FrwC/0lw");
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] receiveData = new byte[256];
            Task queryTask = Task.Run(() =>
            {
                try
                {
                    client.SendTo(sendData, new IPEndPoint(IPAddress.TryParse(address, out IPAddress ipAddress) ? ipAddress : Dns.GetHostAddresses(address).First(), port));
                    client.Receive(receiveData, receiveData.Length, SocketFlags.None);
                }
                catch (Exception) { }
            }
            );
            queryTask.Wait(TimeSpan.FromSeconds(10));
            if (!queryTask.IsCompleted || queryTask.IsFaulted) { throw new ArgumentNullException("Query Failed", "Unable to connect to the server!"); }
            queryTask.Dispose();
            int i = 0;
            return Encoding.UTF8.GetString(receiveData).Substring(31).Split(';').ToDictionary(x => (InfoList)i++, l => /*i == 2 ? System.Text.RegularExpressions.Regex.Replace(l, @"§[A-Za-z\d]", "") :*/ l);
        }
        public static Dictionary<string, string> GetServerInfoD(string address, string port)
        {
            var get = GetServerInfo(address, int.Parse(port));
            return new Dictionary<string, string>() {
                { "type" ,  get[InfoList.type] },
                { "description" ,  get[InfoList.description] },
                { "connectionVer" ,  get[InfoList.connectionVer] },
                { "gameVer" ,  get[InfoList.gameVer] },
                { "onlineplayers" ,  get[InfoList.onlineplayers] },
                { "maxPlayers" ,  get[InfoList.maxPlayers] },
                { "serverUID" ,  get[InfoList.serverUID] },
                { "mapName" ,  get[InfoList.mapName] },
                { "defaultMode" ,  get[InfoList.defaultMode] },
                { "isBDS" ,  get[InfoList.isBDS] },
                { "port" ,  get[InfoList.port] },
                { "portv6" ,  get[InfoList.portv6] },
            };
        }
        #endregion
        //----------------

        public static bool CalculateExpressions(JToken Filters, JToken Source, Dictionary<string, string> Variants) =>
        CalculateExpressions(Filters, Source, Variants, new JObject());
        public static bool CalculateExpressions(JToken Filters, JToken Source, Dictionary<string, string> Variants, JObject server)
        {
            if (Filters.Type == JTokenType.Boolean) { return (bool)Filters; }
            if (Filters.Type == JTokenType.Object)
            {
                if (((JObject)Filters).ContainsKey("any_of"))
                { return ((JArray)((JObject)Filters)["any_of"]).Any(l => CalculateExpressions(l, Source, Variants, server)); }
                else if (((JObject)Filters).ContainsKey("all_of"))
                { return ((JArray)((JObject)Filters)["all_of"]).All(l => CalculateExpressions(l, Source, Variants, server)); }
                else if (((JObject)Filters).ContainsKey("none_of"))
                { return ((JArray)((JObject)Filters)["none_of"]).All(l => !CalculateExpressions(l, Source, Variants, server)); }
                else if (((JObject)Filters).ContainsKey("Path") && ((JObject)Filters).ContainsKey("Operator") && ((JObject)Filters).ContainsKey("Value"))
                {
                    if (((JObject)Filters)["Path"].Type == JTokenType.Array)
                    {
                        string get = null;
                        try { get = GetSubItem(Source, ((JObject)Filters)["Path"].ToList().ConvertAll(l => l.ToString())); }
                        catch (Exception) { }
                        string value = Format(((JObject)Filters)["Value"].ToString(), Variants);
                        //AddLog(get.ToString());
                        //AddLog(value.ToString());
                        switch (((JObject)Filters)["Operator"].ToString())
                        {
                            case "==": case "is": return get == value;
                            case "!=": case "not": return get != value;
                            default:
                                return ValueCompare(get, ((JObject)Filters)["Operator"].ToString(), value);
                        }
                    }
                }
                else if (((JObject)Filters).ContainsKey("Variant") && ((JObject)Filters).ContainsKey("Operator") && ((JObject)Filters).ContainsKey("Value"))
                {
                    if (((JObject)Filters)["Variant"].Type == JTokenType.String)
                    {
                        string get = null;
                        try { get = Variants[((JObject)Filters)["Variant"].ToString()]; }
                        catch (Exception) { }
                        string value = Format(((JObject)Filters)["Value"].ToString(), Variants);
                        switch (((JObject)Filters)["Operator"].ToString())
                        {
                            case "==": case "is": return get == value;
                            case "!=": case "not": return get != value;
                            default:
                                return ValueCompare(get, ((JObject)Filters)["Operator"].ToString(), value);
                        }
                    }
                }
                else if (((JObject)Filters).ContainsKey("ServerConfig") && ((JObject)Filters).ContainsKey("Operator") && ((JObject)Filters).ContainsKey("Value"))
                {
                    string get = null;
                    try
                    { get = GetSubItem(server, ((JObject)Filters)["ServerConfig"].ToList().ConvertAll(l => l.ToString())); }
                    catch (Exception) { }

                    string value = Format(((JObject)Filters)["Value"].ToString(), Variants);
                    switch (((JObject)Filters)["Operator"].ToString())
                    {
                        case "==": case "is": return get == value;
                        case "!=": case "not": return get != value;
                        default:
                            return ValueCompare(get, ((JObject)Filters)["Operator"].ToString(), value);
                    }
                }
            }
            throw new Exception("格式不规范，未能计算Filters的返回值\n" + Filters.ToString());
        }
        private static string GetSubItem(JToken souData, List<string> pathList)
        {
            var operated = souData[pathList.First()];
            pathList.RemoveAt(0);
            if (pathList.Count > 0)
            { return GetSubItem(operated, pathList); }
            else
            {
                if (operated == null) { return null; }
                return operated.ToString();
            }
        }
        #region String转long比较
        private static bool ValueCompare(string Strvalue1, string operation, string Strvalue2)
        {
            try
            {
                long value1 = long.Parse(Strvalue1);
                long value2 = long.Parse(Strvalue2);
                switch (operation)
                {
                    case "<": return value1 < value2;
                    case ">": return value1 > value2;
                    case ">=": return value1 >= value2;
                    case "<=": return value1 <= value2;
                    default:
                        break;
                }
            }
            catch (Exception err) { AddLog($"数值比较器:{err.Message}\n{Strvalue1}{operation}{Strvalue2}"); }
            return false;
        }
        #endregion
        #endregion
        private string CreateUUID() => Guid.NewGuid().ToString();
        public static void AddLog(string text)
        {
            _ = Data.E.CQLog.Info("CQToBDX", text);
        }
        public static string StringToUnicode(string s)//字符串转UNICODE代码
        {
            char[] charbuffers = s.ToCharArray();
            byte[] buffer;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < charbuffers.Length; i++)
            {
                buffer = Encoding.Unicode.GetBytes(charbuffers[i].ToString());
                sb.Append(String.Format("\\u{0:X2}{1:X2}", buffer[1], buffer[0]));
            }
            return sb.ToString();
        }
    }
}

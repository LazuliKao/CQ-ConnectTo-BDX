using Native.Tool.IniConfig.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
                DateTime TimeNow = DateTime.Now;
                double timeI;
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
                    MessageCallback.TimerOrTaskElapsedMessage(Data.TaskTimerActions);
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


        #region 计算操作处理
        public static bool ActionOperation(JObject action, JObject receive, ref Dictionary<string, string> Variants, JObject server)
        {
            if (!action.ContainsKey("Type")) { throw new Exception("参数缺失:\"Type\""); }
            if (!action.ContainsKey("Parameters")) { throw new Exception("参数缺失:\"Parameters\""); }
            JObject Part = action["Parameters"] as JObject;
            if (action.ContainsKey("Filter"))
            { if (!CalculateExpressions(action["Filter"], receive, Variants)) { return true; } }
            switch (action["Type"].ToString().ToLower())
            {
                case "createvariant":
                case "var":
                    try
                    {
                        if (Part.ContainsKey("Path"))
                        {
                            Variants.Add(Part["Name"].ToString(), GetSubItem(receive, Part["Path"].ToList().ConvertAll(l => l.ToString())));
                        }
                        else if (Part.ContainsKey("ServerConfig"))
                        {
                            Variants.Add(Part["Name"].ToString(), GetSubItem(server, Part["ServerConfig"].ToList().ConvertAll(l => l.ToString())));
                        }
                        else if (Part.ContainsKey("Value"))
                        {
                            Variants.Add(Part["Name"].ToString(), Part["Value"].ToString());
                        }
                        else
                        {
                            throw new Exception("缺少参数！创建变量需要\"Path\",\"ServerConfig\"或\"Value\"之一");
                        }
                    }
                    catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "log":
                    try
                    {
                        string logType = "CQToBDX-Log";
                        if (Part.ContainsKey("logType"))
                        {
                            logType = Format(Part["logType"].ToString(), Variants);
                        }
                        if (Part.ContainsKey("Info"))
                            _ = Data.E.CQLog.Info(logType, Format(Part["Info"].ToString(), Variants));
                        if (Part.ContainsKey("Debug"))
                            _ = Data.E.CQLog.Debug(logType, Format(Part["Debug"].ToString(), Variants));
                        if (Part.ContainsKey("Warning"))
                            _ = Data.E.CQLog.Warning(logType, Format(Part["Warning"].ToString(), Variants));
                        if (Part.ContainsKey("Fatal"))
                            _ = Data.E.CQLog.Fatal(logType, Format(Part["Fatal"].ToString(), Variants));
                    }
                    catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "replace":
                    try
                    {
                        if (Part.ContainsKey("CreateVariant"))
                        {
                            Variants.Add(Part["CreateVariant"].ToString(), Variants[Part["TargetVariant"].ToString()].Replace(Part["Find"].ToString(), Part["Replacement"].ToString()));
                        }
                        else
                        {
                            string TargetVariant = Part["TargetVariant"].ToString();
                            Variants[TargetVariant] = Variants[TargetVariant].Replace(Part["Find"].ToString(), Part["Replacement"].ToString());
                        }
                    }
                    catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "regexreplace":
                    try
                    {
                        if (Part.ContainsKey("CreateVariant"))
                        {
                            Variants.Add(Part["CreateVariant"].ToString(), Regex.Replace(Variants[Part["TargetVariant"].ToString()], Part["Pattern"].ToString(), Part["Replacement"].ToString()));
                        }
                        else
                        {
                            string TargetVariant = Part["TargetVariant"].ToString();
                            Variants[TargetVariant] = Regex.Replace(Variants[TargetVariant], Part["Pattern"].ToString(), Part["Replacement"].ToString());
                        }
                    }
                    catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "regexget":
                    try
                    {
                        if (Part.ContainsKey("CreateVariant"))
                        {
                            Variants.Add(Part["CreateVariant"].ToString(), Regex.Match(Variants[Part["TargetVariant"].ToString()], Part["Pattern"].ToString()).Groups[Part["GroupName"].ToString()].Value);
                        }
                        else
                        {
                            string TargetVariant = Part["TargetVariant"].ToString();
                            Variants[TargetVariant] = Regex.Match(Variants[TargetVariant], Part["Pattern"].ToString()).Groups[Part["GroupName"].ToString()].Value;
                        }
                    }
                    catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "format":
                    try
                    {
                        if (Part.ContainsKey("CreateVariant"))
                        {
                            Variants.Add(Part["CreateVariant"].ToString(), Format(Part["Text"].ToString(), Variants));
                        }
                        else
                        {
                            string TargetVariant = Part["TargetVariant"].ToString();
                            Variants[TargetVariant] = Format(Part["Text"].ToString(), Variants);
                        }
                    }
                    catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "tounicode":
                    try
                    {
                        if (Part.ContainsKey("CreateVariant"))
                        {
                            Variants.Add(Part["CreateVariant"].ToString(), StringToUnicode(Format(Part["Text"].ToString(), Variants)));
                        }
                        else
                        {
                            if (Part.ContainsKey("TargetVariant"))
                            {
                                string TargetVariant = Part["TargetVariant"].ToString();
                                Variants[TargetVariant] = StringToUnicode(Variants[TargetVariant]);
                            }
                        }
                    }
                    catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "motdbe":
                    try
                    {
                        if (Part.ContainsKey("CreateVariant"))
                        {
                            try
                            {
                                Variants.Add(Part["CreateVariant"].ToString(),
                                    Format(Format(Part["Text"].ToString(), GetServerInfoD(
                                      Format(Part["IP"].ToString(), Variants),
                                      Format(Part["PORT"].ToString(), Variants)),
                                        "$"), Variants)
                                    );
                            }
                            catch (Exception err)
                            {
                                Variants.Add(Part["CreateVariant"].ToString(), Format(string.Format(Part["FailedText"].ToString(), err.Message), Variants));
                            }
                        }
                    }
                    catch (Exception err)
                    { AddLog($"MotdBE执行出错\nVarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "gethtml":
                    try
                    {
                        if (Part.ContainsKey("CreateVariant"))
                        {
                            try
                            {
                                Native.Tool.Http.HttpWebClient webClient = new Native.Tool.Http.HttpWebClient();
                                Variants.Add(Part["CreateVariant"].ToString(),
                                    Encoding.UTF8.GetString(webClient.DownloadData(Format(Part["URI"].ToString(), Variants)))
                                    );
                            }
                            catch (Exception err)
                            {
                                Variants.Add(Part["CreateVariant"].ToString(), "获取失败" + err.Message);
                            }
                        }
                    }
                    catch (Exception err)
                    { AddLog($"GetHTML执行出错\nVarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "sleep":
                    try
                    {
                        if (Part.ContainsKey("Time"))
                        {
                            Thread.Sleep(int.Parse(Part["Time"].ToString()));
                        }
                    }
                    catch (Exception err)
                    { AddLog($"Sleep执行出错\nVarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "write_ini":
                    try
                    {
                        IniObject iObject = IniObject.Load(Regex.Replace(Part["Path"].ToString(), @"^~/?", Path.GetDirectoryName(Data.ConfigPath) + "/").Replace("/", "\\"));
                        iObject[Format(Part["Section"].ToString(), Variants)][Format(Part["Key"].ToString(), Variants)] = new IniValue(Format(Part["Value"].ToString(), Variants));
                        iObject.Save();
                    }
                    catch (Exception err)
                    { AddLog($"ini写入出错\nVarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "read_ini":
                    try
                    {
                        IniObject iObject = IniObject.Load(Regex.Replace(Part["Path"].ToString(), @"^~/?", Path.GetDirectoryName(Data.ConfigPath) + "/").Replace("/", "\\"));
                        Variants.Add(Part["CreateVariant"].ToString(), iObject[Format(Part["Section"].ToString(), Variants)][Format(Part["Key"].ToString(), Variants)].Value);
                    }
                    catch (Exception err)
                    { AddLog($"ini读取出错\nVarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "write_log":
                    try
                    {
                        File.AppendAllText(Regex.Replace(Part["Path"].ToString(), @"^~/?", Path.GetDirectoryName(Data.ConfigPath) + "/").Replace("/", "\\"),
                                           Format(Part["WriteLine"].ToString(), Variants));
                    }
                    catch (Exception err)
                    { AddLog($"写log操作出错\nVarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "servers":
                    try
                    {
                        Dictionary<string, string> Variants_copy = Variants;
                        string cmdStr = Format(Part["cmd"].ToString(), Variants);
                        foreach (var ws in Part.ContainsKey("Filter")
                                                     ? Data.WSClients.Where(l => l.Key.IsAlive && CalculateExpressions(Part["Filter"], receive, Variants_copy, l.Value))
                                                     : Data.WSClients.Where(l => l.Key.IsAlive))
                        {
                            ws.Key.Send(Part.ContainsKey("CallbackActions")
                                                ? Data.GetCmdReq(ws.Value["Passwd"].ToString(), cmdStr, (JArray)Part["CallbackActions"], Variants_copy)
                                                : Data.GetCmdReq(ws.Value["Passwd"].ToString(), cmdStr));
                        }
                    }
                    catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
                case "privatemessage":
                    try
                    {
                        Data.E.CQApi.SendPrivateMessage(long.Parse(Format(Part["QQ"].ToString(), Variants)), Format(Part["Message"].ToString(), Variants));
                    }
                    catch (Exception err) { throw new Exception($"VarCount:{Variants.Count}\n位于{action}\n错误内容{err.Message}"); }
                    break;
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
                    return true;
            }
            return false;
        }
        #endregion

        #region 3.字符串格式化
        #region format
        public static string Format(string input, Dictionary<string, string> vars)
        {
            if (string.IsNullOrEmpty(input)) { return ""; }
            string processText = input;/*Format(input,new Dictionary<string, string>() { { "Time"}, });*/
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
        public static string RandomUUID
        {
            get { return Guid.NewGuid().ToString(); }
        }
        public static string GetMD5(string sDataIn)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
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

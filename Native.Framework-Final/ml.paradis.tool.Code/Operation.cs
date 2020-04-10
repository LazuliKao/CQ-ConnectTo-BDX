using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace ml.paradis.tool.Code
{
    class Operation
    {
        private static void ReceiveMessage(WebSocket wsc, string receiveData)
        {
            try
            {
                JObject receive = JObject.Parse(receiveData);
                foreach (JObject server in Data.Config["Servers"])
                {
                    void DoTriggers(JArray triggers)
                    {
                        foreach (JObject trigger in triggers)
                        {
                            VariantCreate(trigger, receive, out Dictionary<string, string> Variants, server);
                            VariantOperation(trigger, receive, ref Variants);
                            #region 过滤条件计算
                            if (trigger.ContainsKey("Filter"))
                            {
                                void DoActions(JObject action)
                                {
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
                                        case "sender":
                                            if (action.ContainsKey("cmd"))
                                            {
                                                wsc.Send(Data.GetCmdReq(server["Passwd"].ToString(), Format(action["cmd"].ToString(), Variants)));
                                            }
                                            break;
                                        case "other":
                                            foreach (WebSocket ws in Data.WSClients.Keys.Where(l => l != wsc && l.IsAlive))
                                            {
                                                if (action.ContainsKey("cmd"))
                                                { ws.Send(Data.GetCmdReq(server["Passwd"].ToString(), Format(action["cmd"].ToString(), Variants))); }
                                            }
                                            break;
                                        case "all":
                                            foreach (WebSocket ws in Data.WSClients.Keys.Where(l => l.IsAlive))
                                            {
                                                if (action.ContainsKey("cmd"))
                                                { ws.Send(Data.GetCmdReq(server["Passwd"].ToString(), Format(action["cmd"].ToString(), Variants))); }
                                            }
                                            break;
                                        case "QQGroup":
                                            if (action.ContainsKey("GroupID"))
                                            {
                                                Data.E.CQApi.SendGroupMessage(long.Parse(action["GroupID"].ToString()), Format(action["Message"].ToString(), Variants));
                                            }
                                            break;
                                        case "doTriggers":
                                            DoTriggers((JArray)action["Triggers"]);
                                            break;
                                        default:
                                            break;
                                    }

                                }
                                if (CalculateExpressions(trigger["Filter"], receive, Variants))
                                {
                                    #region 满足条件Actions 
                                    if (trigger.ContainsKey("Actions"))
                                    {
                                        foreach (JObject action in trigger["Actions"])
                                        {
                                            DoActions(action);
                                        }
                                    }
                                    #endregion
                                }
                                else
                                {
                                    #region 不满足条件Actions 
                                    if (trigger.ContainsKey("MismatchedActions"))
                                    {
                                        foreach (JObject action in trigger["MismatchedActions"])
                                        {
                                            DoActions(action);
                                        }
                                    }
                                    #endregion
                                }
                            }
                            #endregion
                        }
                    }
                    DoTriggers((JArray)server["Triggers"]);
                }
            }
            catch (Exception err)
            { AddLog(err.ToString()); }
        }
        public static void Setup()
        {
            foreach (var server in Data.Config["Servers"])
            {
                Data.WSClients.Add(new WebSocket(server["Address"].ToString()), (JObject)server);
                Data.WSClients.Last().Key.OnMessage += (sender, e) =>
                {
                    AddLog((sender as WebSocket).Url + "Received:" + e.Data);
                    ReceiveMessage(sender as WebSocket, e.Data);
                };
                Data.WSClients.Last().Key.OnClose += (sender, e) =>
                {
                    AddLog((sender as WebSocket).Url + "连接已断开:" + e.Code + "\n" + e.Reason);
                };
                Data.WSClients.Last().Key.OnError += (sender, e) =>
                {
                    AddLog((sender as WebSocket).Url + "运行出错:" + e.Exception + "\n" + e.Message);
                };
                Data.WSClients.Last().Key.OnOpen += (sender, e) =>
                {
                    AddLog((sender as WebSocket).Url + "连接已建立");
                };
                try
                {
                    Data.WSClients.Last().Key.Connect();
                    if (Data.WSClients.Last().Key.IsAlive)
                    {
                        AddLog($"{server["Tag"]}{Data.WSClients.Last().Key.Url}连接成功！");
                    }
                    else
                    {
                        AddLog($"{server["Tag"]}{Data.WSClients.Last().Key.Url}连接失败！");
                    }
                }
                catch (Exception err) { AddLog($"{server["Tag"]}{Data.WSClients.Last().Key.Url}连接失败！\n{err}"); }
            }
            Data.KeepAliveTimer.Interval = double.Parse(Data.Config["CheckConnectionTime"].ToString()) * 1000;
            Data.KeepAliveTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                foreach (WebSocket ws in Data.WSClients.Keys.Where(l => !l.IsAlive))
                {
                    try
                    {
                        ws.Connect();
                        if (ws.Ping() && ws.IsAlive)
                        { AddLog($"[KeepAlive]实例重连成功！\n" + ws.Url); }
                        else
                        { AddLog($"[KeepAlive]实例重连失败！\n" + ws.Url); }
                    }
                    catch (Exception err) { AddLog($"重连失败！{ws.Url}\n{err}"); }
                }
            };
            Data.KeepAliveTimer.Start();
        }
        public static void Quit()
        {
            Data.KeepAliveTimer.Stop();
            foreach (var item in Data.WSClients.Keys)
            {
                item.Close();
            }
        }
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
                            switch (operation["Type"].ToString())
                            {
                                case "Replace":
                                    try
                                    {
                                        if (Operation.CalculateExpressions(operation["Filter"], receive, Variants))
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
                                    }
                                    catch (Exception)
                                    { AddLog("参数缺失或变量不存在"); }
                                    break;
                                case "RegexReplace":
                                    try
                                    {
                                        if (CalculateExpressions(operation["Filter"], receive, Variants))
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
                                    }
                                    catch (Exception)
                                    { AddLog("参数缺失或变量或正则表达式有误不存在"); }
                                    break;
                                case "RegexGet":
                                    try
                                    {
                                        if (CalculateExpressions(operation["Filter"], receive, Variants))
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
                                    }
                                    catch (Exception)
                                    { AddLog("参数缺失或变量或正则表达式有误不存在"); }
                                    break;
                                case "Format":
                                    try
                                    {
                                        if (CalculateExpressions(operation["Filter"], receive, Variants))
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
                                    }
                                    catch (Exception)
                                    { AddLog("参数缺失或变量或格式有误不存在"); }
                                    break;
                                case "ToUnicode":
                                    try
                                    {
                                        if (CalculateExpressions(operation["Filter"], receive, Variants))
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
                                    }
                                    catch (Exception)
                                    { AddLog("参数缺失或变量或格式有误不存在"); }
                                    break;
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
        #endregion

        #region 3.字符串格式化
        #region format
        public static string Format(string input, Dictionary<string, string> vars)
        {
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
        #endregion

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
            throw new Exception("格式不规范，未能计算Filters的返回值\n"+Filters.ToString());
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
                buffer = System.Text.Encoding.Unicode.GetBytes(charbuffers[i].ToString());
                sb.Append(String.Format("\\u{0:X2}{1:X2}", buffer[1], buffer[0]));
            }
            return sb.ToString();
        }

    }
}

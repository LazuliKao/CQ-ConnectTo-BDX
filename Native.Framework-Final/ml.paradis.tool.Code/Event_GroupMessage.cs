using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;
using Newtonsoft.Json.Linq;

namespace ml.paradis.tool.Code
{
    public class Event_GroupMessage : IGroupMessage
    {
        private string GetMemberNick(ref CQGroupMessageEventArgs e)
        {
            return GetMemberNick(ref e, e.FromQQ.Id);
        }
        private string GetMemberNick(ref CQGroupMessageEventArgs e, long QQid)
        {
            try
            {
                string nick = e.CQApi.GetGroupMemberInfo(e.FromGroup.Id, QQid, true).Card;
                nick = string.IsNullOrEmpty(nick) ? e.CQApi.GetGroupMemberInfo(e.FromGroup.Id, QQid, true).Nick : nick;
                short signIndex = short.MaxValue;
                foreach (var sign in new char[] { '(', '/', '[', '{', '【', '（', ':' })
                {
                    short index = (short)nick.IndexOf(sign);
                    if (index != -1) { signIndex = Math.Min(signIndex, index); }
                }
                return signIndex == short.MaxValue ? nick : nick.Remove(signIndex);
            }
            catch (Exception)
            { return "null"; }
        }
        private void ReturnGMAtFrom(ref CQGroupMessageEventArgs e, string message)
        {
            if (message == null) {/* message = "null";*/return; }
            message = Regex.Replace(message, "\\s$", "");
            e.CQApi.SendGroupMessage(e.FromGroup, Native.Sdk.Cqp.CQApi.CQCode_At(e.FromQQ), message);
        }
        private void ReturnPrivateMessage(ref CQGroupMessageEventArgs e, string message)
        {
            if (message == null) { message = "null"; }
            message = Regex.Replace(message, "\\s$", "");
            e.CQApi.SendPrivateMessage(e.FromQQ, message);
        }

        /// <summary>
        /// 收到群消息
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件参数</param>
        public void GroupMessage(object sender, CQGroupMessageEventArgs e)
        {
            //// 获取 At 某人对象
            //CQCode cqat = e.FromQQ.CQCode_At();
            //// 往来源群发送一条群消息, 下列对象会合并成一个字符串发送
            //e.FromGroup.SendGroupMessage(cqat, " 您发送了一条消息: ", e.Message);

            // 设置该属性, 表示阻塞本条消息, 该属性会在方法结束后传递给酷Q

            if (((JArray)Data.Config["Groups"]).ToList().Any(l => long.Parse(l["ID"].ToString()) == e.FromGroup.Id))
            {
                foreach (JObject group in ((JArray)Data.Config["Groups"]).Where(l => long.Parse(l["ID"].ToString()) == e.FromGroup.Id))
                {
                    #region 消息转发
                    try
                    {
                        string output_text = CQApi.CQDeCode(e.Message.Text);
                        #region 转发条件检查
                        bool CheckCondition()
                        {
                            if (!(bool)group["SendToServer"]) { return false; }
                            if (group.ContainsKey("SendToServerRegex"))
                            {
                                Match MSGCheck = Regex.Match(output_text, group["SendToServerRegex"].ToString());
                                if (MSGCheck.Success) { output_text = MSGCheck.Value; }
                                else { e.CQLog.Debug("群聊=>服务器", "消息不符合正则表达式,未转发"); return false; }
                            }
                            return true;
                        }
                        #endregion
                        if (CheckCondition())
                        {
                            JObject Format = group["SendToServerFormat"] as JObject;
                            foreach (var item in e.Message.CQCodes)
                            {
                                try
                                {
                                    switch (item.Function)
                                    {
                                        case CQFunction.At://[CQ:at,qq=441870948]
                                            output_text = output_text.Replace(item.ToString() + " ", string.Format(Format["CQAt"].ToString(), GetMemberNick(ref e, Convert.ToInt64(item.Items["qq"]))));
                                            output_text = output_text.Replace(item.ToString(), string.Format(Format["CQAt"].ToString(), GetMemberNick(ref e, Convert.ToInt64(item.Items["qq"]))));
                                            break;
                                        case CQFunction.Image://[CQ: image, file=2031F3F5C7B5CEB95725CB27B76BC1AD.jpg]
                                            output_text = output_text.Replace(item.ToString(), Format["CQImage"].ToString());
                                            break;
                                        case CQFunction.Emoji://[CQ: emoji, id=128560]
                                            output_text = output_text.Replace(item.ToString(), Format["CQEmoji"].ToString());
                                            break;
                                        case CQFunction.Face://[CQ:face,id=14]
                                            output_text = output_text.Replace(item.ToString(), Format["CQFace"].ToString());
                                            break;
                                        case CQFunction.Bface:
                                            //[CQ:bface,p=10616,id=F4A89A4173A48888751D27679FFEEB60]&#91;财源广进&#93;
                                            Match m = Regex.Match(output_text, "\\[(.*?)\\]");
                                            output_text = output_text.Replace(item.ToString(), string.Format(Format["CQBface"].ToString(), m.Groups[1]).Replace(m.Value, null));
                                            break;
                                        //case CQFunction.Sign:
                                        //    output_text = output_text.Replace(item.ToString(), "§l§7[签到]§r§a");
                                        //    break;
                                        default:
                                            break;
                                    }
                                }
                                catch (Exception) { continue; }
                            }
                            output_text = output_text.Replace("\r", "")/*.Replace("&#91;", "[").Replace("&#93;", "]")*/;
                            output_text = string.Format(Format["Main"].ToString(), GetMemberNick(ref e), output_text);   // $"§b【群聊消息】§e<{GetMemberNick(ref e)}>§a{output_text}";
                            e.CQLog.Info("转发消息到服务器", output_text);
                            foreach (var server in Data.WSClients.Where(l => l.Key.IsAlive))
                            {
                                server.Key.Send(Data.GetCmdReq(server.Value["Passwd"].ToString(), $"tellraw @a {{\"rawtext\":[{{\"text\":\"{Operation.StringToUnicode(output_text)}\"}}]}}"));
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        Operation.AddLog("群聊消息转发" + err.ToString());
                    }
                    #endregion

                    #region 触发器匹配
                    try
                    {
                        var MemberInfo = e.FromQQ.GetGroupMemberInfo(e.FromGroup);
                        var GroupInfo = e.FromGroup.GetGroupInfo();
                        JObject receive = new JObject() {
                            new JProperty("Message", CQApi.CQDeCode(e.Message.Text)/*.Replace("&#91;", "[").Replace("&#93;", "]")*/),
                            new JProperty("FromQQ", e.FromQQ.Id),
                            new JProperty("FromQQNick",MemberInfo.Nick),
                            new JProperty("FromGroup", e.FromGroup.Id),
                            new JProperty("IsFromAnonymous", e.IsFromAnonymous),
                            new JProperty("Id", e.Id),
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
                        //   Operation.AddLog(receive.ToString());
                        DoTriggers((JArray)group["Triggers"], ref receive, group, ref e);
                    }
                    catch (Exception err)
                    { Operation.AddLog("群聊触发器" + err.ToString()); }
                    #endregion

                }

            }
        }
        private void DoTriggers(JArray triggers, ref JObject receive, JObject group, ref CQGroupMessageEventArgs e)
        {
            foreach (JObject trigger in triggers)
            {
                try
                {
                    Operation.VariantCreate(trigger, receive, out Dictionary<string, string> Variants, group);
                    Operation.VariantOperation(trigger, receive, ref Variants);
                    #region 过滤条件计算
                    if (trigger.ContainsKey("Filter"))
                    {
                        if (Operation.CalculateExpressions(trigger["Filter"], receive, Variants))
                        {
                            #region 满足条件的Actions
                            if (trigger.ContainsKey("Actions"))
                            {
                                foreach (JObject action in trigger["Actions"])
                                {
                                    DoAction(action, ref Variants, ref receive, ref group, ref e);
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            #region 不满足条件的Actions
                            if (trigger.ContainsKey("MismatchedActions"))
                            {
                                foreach (JObject action in trigger["MismatchedActions"])
                                {
                                    DoAction(action, ref Variants, ref receive, ref group, ref e);
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion
                }
                catch (Exception err)
                { Operation.AddLog("触发器解析" + err.ToString()); }
            }
        }
        private void DoAction(JObject action, ref Dictionary<string, string> Variants, ref JObject receive, ref JObject group, ref CQGroupMessageEventArgs e)
        {
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
                            var lambda_receive = receive;
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
                case "ReturnGroupMessageAtFrom":
                    if (action.ContainsKey("Message"))
                    {
                        ReturnGMAtFrom(ref e, Operation.Format(action["Message"].ToString(), Variants));
                    }
                    break;
                case "ReturnPrivateMessage":
                    if (action.ContainsKey("Message"))
                    {
                        ReturnPrivateMessage(ref e, Operation.Format(action["Message"].ToString(), Variants));
                    }
                    break;
                case "doTriggers":
                    DoTriggers((JArray)action["Triggers"], ref receive, group, ref e);
                    break;
                default:
                    break;
            }
        }
    }
}

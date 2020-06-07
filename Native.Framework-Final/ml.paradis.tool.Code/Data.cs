using Native.Sdk.Cqp.EventArgs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using WebSocketSharp;

namespace ml.paradis.tool.Code
{
    public class Data
    {
        public static Dictionary<Timer, JObject> Timers = new Dictionary<Timer, JObject>();
        public static Timer KeepAliveTimer = new Timer() { AutoReset = true };
        public static Timer TaskTimer = new Timer() { AutoReset = false };
        public static JObject TaskTimerActions = new JObject();
        public static CQAppEnableEventArgs E = null;
        private static string ConfigPathData = null;//数据文件储存路径
        public static string ConfigPath
        {
            get
            {
                if (ConfigPathData == null) { ConfigPathData = E.CQApi.AppDirectory.Replace("\\data\\app\\", "\\app\\") + "config.json"; }
                return ConfigPathData;
            }
        }
        private static JObject configData = null;
        public static JObject Config
        {
            get
            {
                if (configData == null)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(ConfigPath)))//检测文件目录是否存在
                    { Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)); }
                    if (!File.Exists(ConfigPath))
                    {
                        File.WriteAllBytes(ConfigPath, Code.Config.config);
                    }
                    configData = JObject.Parse(File.ReadAllText(ConfigPath));
                    try
                    {
                        if ((double)configData["configVersion"] < (double)JObject.Parse(Encoding.UTF8.GetString(Code.Config.config))["configVersion"])
                        {
                            try
                            {
                                File.Copy(ConfigPath, Path.GetDirectoryName(ConfigPath) + "\\config_old.json");
                            }
                            catch (Exception)
                            {
                                for (int i = 1; i < 500; i++)
                                {
                                    try
                                    { File.Copy(ConfigPath, Path.GetDirectoryName(ConfigPath) + "\\config_old (" + i + ").json"); break; }
                                    catch (Exception)
                                    { continue; }
                                }
                            }
                            File.WriteAllBytes(ConfigPath, Code.Config.config);
                        }
                    }
                    catch (Exception err) { Operation.AddLog(err.ToString()); }
                }
                return configData;
            }
            set { configData = value; }
        }
        public static Dictionary<WebSocket, JObject> WSClients = new Dictionary<WebSocket, JObject>();
        public struct CallBackInfo
        {
            public string uuid;
            public JArray CallbackActions;
            public Dictionary<string, string> Variants;
        }
        public static List<CallBackInfo> CMDQueue = new List<CallBackInfo>();
        public static string GetCmdReq(string token, string cmd, JArray CBactions, Dictionary<string, string> Variants)
        {
            JObject raw = new JObject() {
                new JProperty("operate","runcmd"),
                new JProperty("cmd",cmd),
                new JProperty("msgid", Operation.RandomUUID)
            };
            raw.Add("passwd", Operation.GetMD5(token + DateTime.Now.ToString("yyyyMMddHHmm") + "@" + raw.ToString(Newtonsoft.Json.Formatting.None)));
            Dictionary<string, string> Variants_copy = new Dictionary<string, string>();
            foreach (var variant in Variants)
            {
                Variants_copy.Add(variant.Key, variant.Value);
            }
            CMDQueue.Add(new CallBackInfo()
            {
                uuid = raw["msgid"].ToString(),
                CallbackActions = CBactions,
                Variants = Variants_copy
            });
            return raw.ToString(Newtonsoft.Json.Formatting.None);
        }
        public static string GetCmdReq(string token, string cmd)
        {
            JObject raw = new JObject() {
                new JProperty("operate","runcmd"),
                new JProperty("cmd",cmd),
                new JProperty("msgid","0")
            };
            raw.Add("passwd", Operation.GetMD5(token + DateTime.Now.ToString("yyyyMMddHHmm") + "@" + raw.ToString(Newtonsoft.Json.Formatting.None)));
            return raw.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}

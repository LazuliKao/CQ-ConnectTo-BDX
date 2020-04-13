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
        public static Timer KeepAliveTimer = new Timer() { AutoReset = true };
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
        private readonly static JObject cmdModel = JObject.Parse("{\"operate\":\"runcmd\",\"passwd\":\"token\",\"cmd\":\"say null\"}");
        public static string GetCmdReq(string token, string cmd)
        {
            string GetMD5(string sDataIn)
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
            JObject raw = cmdModel;
            raw["cmd"] = cmd;
            raw["passwd"] = GetMD5(token + DateTime.Now.ToString("yyyyMMddHHmm"));
            return raw.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}

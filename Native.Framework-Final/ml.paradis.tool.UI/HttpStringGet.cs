using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
namespace ml.paradis.tool.UI
{
    class HttpStringGet
    {
        /// <summary>  
        /// 获取网页的HTML
        /// </summary>  
        /// <param name="url">链接地址:string</param>  
        /// <returns></returns>  
        public static string GetHtmlStr(string url) => GetHtmlStr(url, Encoding.Default);

        /// <summary>  
        /// 获取网页的HTML
        /// </summary>  
        /// <param name="url">链接地址:string</param>
        /// <param name="encoding">编码类型:Encoding</param>
        /// <returns></returns>  
        public static string GetHtmlStr(string url, Encoding encoding) => GetHtmlStr(url, encoding, Reqheaders: null);

        /// <summary>  
        /// 获取网页的HTML
        /// </summary>  
        /// <param name="url">链接地址:string</param>  
        /// <param name="encoding">编码类型:Encoding</param>  
        /// <param name="Reqheaders">请求头:Dictionary<HttpRequestHeader, string></param>  
        ///// <param name="Resheaders">响应头:Dictionary<HttpRequestHeader, string></param>  
        /// <returns></returns>  
        public static string GetHtmlStr(string url, Encoding encoding, string Reqheaders)
        {
            try
            {
                if (!string.IsNullOrEmpty(url))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            WebRequest request = WebRequest.Create(url);            //实例化WebRequest对象
                            //foreach (var hd in Reqheaders)
                            //{
                            if (!string.IsNullOrEmpty(Reqheaders))
                            {
                                request.Headers.Add(Reqheaders);
                            }
                            //}
                            //foreach (var hd in Resheaders)
                            //{
                            //    request.Headers.Add(hd.Key, hd.Value);
                            //}
                            request.Timeout = 10000;
                            WebResponse response = request.GetResponse();           //创建WebResponse对象  
                            Stream datastream = response.GetResponseStream();   //创建流对象  
                            StreamReader reader = new StreamReader(datastream, encoding);
                            try
                            {
                                return reader.ReadToEnd();                                      //读取网页内容  
                            }
                            finally
                            {
                                reader.Close();
                                datastream.Close();
                                response.Close();
                            }
                        }
                        catch (Exception err)
                        {
                            if (i == 5)
                            { return "Error{" + err.Message + "}"; }
                            continue;
                        }
                    }
                }
            }
            catch { }
            return "Error!";
        }
        ///// <summary>  
        ///// 获取网页的HTML
        ///// </summary>  
        ///// <param name="url">链接地址:string</param>  
        ///// <param name="encoding">编码类型:Encoding</param>  
        ///// <param name="headers">请求头:Dictionary<HttpRequestHeader, string></param>  
        ///// <returns></returns>  
        //public static string GetHtmlStr(string url, Encoding encoding, string headers)
        //{
        //    return;
        //}


        public static bool HttpFileExist(string fileUrl)
        {
            try
            {
                //创建根据网络地址的请求对象
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.CreateDefault(new Uri(fileUrl));
                httpWebRequest.Method = "HEAD";
                httpWebRequest.Timeout = 1000;
                //返回响应状态是否是成功比较的布尔值
                return (((HttpWebResponse)httpWebRequest.GetResponse()).StatusCode == System.Net.HttpStatusCode.OK);
                //using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                //{
                //    return response.StatusCode == HttpStatusCode.OK;
                //}
            }
            catch (Exception)
            {
                return false;
            }
        }



    }
}

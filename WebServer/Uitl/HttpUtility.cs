using System.Web.Mvc;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System;
using System.Web.Script.Serialization;

namespace WebServer
{
    public class HttpUtility
    {
        static private HttpStatusCode HttpDelete(string url, string path, out string result)
        {
            HttpWebRequest req = WebRequest.Create(url + '/' + path) as HttpWebRequest;
            req.Method = "DELETE";
            HttpWebResponse resp = req.GetResponse() as HttpWebResponse;

            StreamReader reader = new StreamReader(resp.GetResponseStream());

            result = reader.ReadToEnd();
            return resp.StatusCode;
        }

        static public HttpStatusCode HttpDelete(string url, string path)
        {
            HttpWebRequest req = WebRequest.Create(url + '/' + path) as HttpWebRequest;
            req.Method = "DELETE";
            HttpWebResponse resp = req.GetResponse() as HttpWebResponse;

            return resp.StatusCode;
        }

        static public async Task<T> HttpGet<T>(string url, params string[] parameter)
        {
            string path = url;
            foreach (string s in parameter)
            {
                path += "/" + s;
            }

            HttpWebRequest req = WebRequest.Create(path) as HttpWebRequest;
            req.Method = "GET";
            req.Timeout = 3000;
            var resp = await req.GetResponseAsync() as HttpWebResponse;

            StreamReader reader = new StreamReader(resp.GetResponseStream());

            string result = await reader.ReadToEndAsync();
            return NetSerializer.ToObject<T>(result);
        }

        static public async Task<string> HttpGet2(string url, params string[] parameter)
        {
            string path = url;
            foreach (string s in parameter)
            {
                path += "/" + s;
            }

            HttpWebRequest req = WebRequest.Create(path) as HttpWebRequest;
            req.Method = "GET";
            var resp = await req.GetResponseAsync() as HttpWebResponse;

            StreamReader reader = new StreamReader(resp.GetResponseStream());

            string result = await reader.ReadToEndAsync();
            return result;
        }

        static public async Task<HttpWebResponse> HttpPostAsync(string url, object parameter)
        {
            string resultValues = NetSerializer.ToJson(parameter);

            byte[] bytes = Encoding.UTF8.GetBytes(resultValues);

            HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
            req.Method = "POST";
            req.ContentLength = bytes.Length;
            req.ContentType = "application/json";
            req.Timeout = 20000;
            req.ReadWriteTimeout = 5000;


            var reqStream = await req.GetRequestStreamAsync();
            reqStream.Write(bytes, 0, bytes.Length);
            reqStream.Close();

            HttpWebResponse resp = null;
            resp = req.GetResponse() as HttpWebResponse;

            return resp;
        }

        static public HttpWebResponse HttpGet(string url, params string[] parameter)
        {
            string path = url;
            foreach (string s in parameter)
            {
                path += "/" + s;
            }

            HttpWebRequest req = WebRequest.Create(path) as HttpWebRequest;
            req.Method = "GET";
            req.Timeout = 2000;
            return req.GetResponse() as HttpWebResponse;
        }

        static public HttpWebResponse HttpPostPerform(string url, object parameter, bool security = false)
        {
            string resultValues = NetSerializer.ToJson(parameter);

            byte[] bytes = Encoding.UTF8.GetBytes(resultValues);

            HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
            req.Method = "POST";
            req.ContentLength = bytes.Length;
            req.ContentType = "application/json";
            req.Timeout = -1;
            var reqStream = req.GetRequestStream();
            reqStream.Write(bytes, 0, bytes.Length);
            reqStream.Close();

            HttpWebResponse resp = null;
            resp = req.GetResponse() as HttpWebResponse;

            return resp;
        }

        static public string HttpPostBase(string url, object parameter, bool security = false)
        {
            HttpWebResponse resp = HttpPostPerform(url, parameter, security);
            StreamReader reader = new StreamReader(resp.GetResponseStream());

            string result = reader.ReadToEnd();
            return result;
        }

        static public T HttpPost<T>(string url, object parameter, bool security = false)
        {
            string result = HttpPostBase(url, parameter, security);
            return NetSerializer.ToObject<T>(result);
        }

        static public HttpStatusCode HttpPost(string url, object parameter)
        {
            HttpWebResponse resp = HttpPostPerform(url, parameter);
            return resp.StatusCode;
        }

        static public T HttpPost<T>(string url, object parameter, params string[] path)
        {
            string path2 = url;
            foreach (string s in path)
            {
                path2 += "/" + s;
            }

            return HttpPost<T>(path2, parameter);
        }

        static public string HttpPostToString(string url, object parameter, bool security = false, params string[] path)
        {
            string path2 = url;
            foreach (string s in path)
            {
                path2 += "/" + s;
            }

            String result = HttpPostBase(path2, parameter, security);
            if (security && result.Length > 0)
            {
            }

            return result;
        }

        static public HttpStatusCode HttpPost(string url, object parameter, params string[] path)
        {
            string path2 = url;
            foreach (string s in path)
            {
                path2 += "/" + s;
            }

            return HttpPost(path2, parameter);
        }

        static public string MakePath(string url, params string[] parameter)
        {
            string path = url;
            foreach (string s in parameter)
            {
                path += "/" + s;
            }

            return path;
        }

        static public ContentResult ToLongJson(object data)
        {
            var serializer = new JavaScriptSerializer();
            var result = new ContentResult();
            serializer.MaxJsonLength = Int32.MaxValue; // Whatever max length you want here
            result.Content = serializer.Serialize(data);
            result.ContentType = "application/json";

            return result;
        }
    }
}
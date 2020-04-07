using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.IO;

namespace WebServer
{
    public class ServerConfig : JsonConfig<ServerConfig>
    {
        public string BindRestApiUrl = "http://localhost:10082";
        public string BindDeepLearningModelUrl = "http://localhost:10082";
        public string DBConnectionString = "Server=localhost;Database=name;User Id=id;Password=pw;";
        public int DBaccessPermitSize = 3;
        public int ThreadPoolSize = 3;
    }

    public class JsonConfig<T> : Singleton<JsonConfig<T>>
    {
        public T Data;
        private string _filename;

        public bool Load(string filename)
        {
            if (!File.Exists(filename))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                string content = Util.ToJson(Data);
                File.WriteAllText(filename, content);
            }

            TextReader r = File.OpenText(filename);
            JsonReader reader = new JsonTextReader(r);
            JsonSerializer serializer = new JsonSerializer();
            Data = serializer.Deserialize<T>(reader);
            _filename = filename;
            r.Close();
            return true;
        }

        public void Save()
        {
            string content = Util.ToJson(Data);
            File.WriteAllText(_filename, content);
        }
    }
}
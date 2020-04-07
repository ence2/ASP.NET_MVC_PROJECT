using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Security.Cryptography;
using System;
using System.Text;
using System.IO;
#if SERVER_SIDE
using System.IO.Compression;
#endif

namespace WebServer
{
    public class NetSerializer
    {
#if SERVER_SIDE
        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[MAX_PACKET_SIZE];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static byte[] Zip(object obj)
        {
            return Zip(ToJson(obj));
        }

        public static T Unzip<T>(byte[] bytes)
        {
            var str = Unzip(bytes);
            return ToObject<T>(str);
        }

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        const int MAX_PACKET_SIZE = 463840;
        public static String Compress(String message)
        {
            byte[] compressed = Zip(message);
            return Convert.ToBase64String(compressed, 0, compressed.Length);
        }
#endif

#if SERVER_SIDE
        public static String Decompress(String message)
        {
            byte[] bin = Convert.FromBase64String(message);
            return Unzip(bin);
        }
#endif

        public static object ToObject(string json, Type type)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);
        }

        public static T ToObject<T>(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

#if SERVER_SIDE
        public static T ToDecompressedObject<T>(String json)
        {
            JsonSerializerSettings s = new JsonSerializerSettings();
            s.MissingMemberHandling = MissingMemberHandling.Ignore;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Decompress(json), s);
        }
#endif

        public static string ToJson(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, Formatting.None, new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
        }

#if SERVER_SIDE
        public static String ToCompressedJson(object obj)
        {
            if (obj == null)
                return string.Empty;

            return Compress(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
        }
#endif
    }

    public class Util
    {
        public static string MakeKey(string appId, string deviceToken)
        {
            return appId + "_" + deviceToken;
        }

        static public Uri GetFindLocalIP(String url)
        {
            var urlFormat = url.Split(new string[] { "://", ":" }, StringSplitOptions.RemoveEmptyEntries);
            String host = urlFormat[1];
            String schema = urlFormat[0];
            String port = urlFormat[2];

            String combine = "";
            System.Net.IPHostEntry host2;

            if (urlFormat[1] != "localhost")
            {
                String[] ipClasses = host.Split(new char[] { '.' });
                if (ipClasses.Length < 4)
                {
                    host2 = System.Net.Dns.GetHostEntry(host);
                }
                else
                {
                    if (ipClasses[2] == "*")
                        combine = String.Format("{0}.{1}", ipClasses[0], ipClasses[1]);
                    else if (ipClasses[3] == "*")
                        combine = String.Format("{0}.{1}.{2}", ipClasses[0], ipClasses[1], ipClasses[2]);
                    else
                    {
                        UriBuilder uri = new UriBuilder(url);
                        return uri.Uri;
                    }
                    host2 = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                }
            }
            else
            {
                host2 = System.Net.Dns.GetHostEntry(host);
            }

            string clientIP = string.Empty;
            for (int i = 0; i < host2.AddressList.Length; i++)
            {
                if (host2.AddressList[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    clientIP = host2.AddressList[i].ToString();

                    if (clientIP.StartsWith(combine))
                    {
                        UriBuilder uri = new UriBuilder(schema, clientIP, Int32.Parse(port));
                        return uri.Uri;
                    }
                }
            }

            throw new Exception("Not found host");
        }

        public static string FindLocalIP(string url)
        {
            UriBuilder builder = new UriBuilder(url);

            if (builder.Host == "localhost")
            {
                builder.Host = "127.0.0.1";
            }

            string[] ipClasses = builder.Host.Split(new char[] { '.' });
            string combine = "";
            if (ipClasses[2] == "*")
                combine = string.Format("{0}.{1}", ipClasses[0], ipClasses[1], ipClasses[2]);
            else if (ipClasses[3] == "*")
                combine = string.Format("{0}.{1}.{2}", ipClasses[0], ipClasses[1], ipClasses[2]);
            else
                return builder.ToString();

            System.Net.IPHostEntry hostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            string clientIP = string.Empty;
            for (int i = 0; i < hostEntry.AddressList.Length; i++)
            {
                if (hostEntry.AddressList[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    clientIP = hostEntry.AddressList[i].ToString();

                    if (clientIP.StartsWith(combine))
                    {
                        builder.Host = clientIP;
                        return builder.ToString();
                    }
                }
            }

            throw new Exception("Not found host");
        }

        public static object ToObject(string json, Type type)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);
        }

        public static T ToObject<T>(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        public static string ToJson(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, Formatting.None, new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
        }

        public static string Encrypt(string input)
        {
            return AES256.Encrypt(input, "wqklj#$%35&^DFs2jff1123456789dft");
        }

        public static string Decrypt(string input)
        {
            return AES256.Decrypt(input, "wqklj#$%35&^DFs2jff1123456789dft");
        }

        public static DateTime Next(DateTime from, DayOfWeek dayOfWeek)
        {
            int start = (int)from.DayOfWeek;
            int target = (int)dayOfWeek;
            if (target <= start)
                target += 7;
            return from.AddDays(target - start);
        }

        public static int RoundToInt(float f)
        {
            return (int)Math.Round((double)f);
        }
    }

    public class AES256
    {
        public static string Encrypt(string Input, string key)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            var encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] xBuff = null;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Encoding.UTF8.GetBytes(Input);
                    cs.Write(xXml, 0, xXml.Length);
                }

                xBuff = ms.ToArray();
            }

            string Output = Convert.ToBase64String(xBuff);
            return Output;
        }

        public static string Decrypt(string Input, string key)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            var decrypt = aes.CreateDecryptor();
            byte[] xBuff = null;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Convert.FromBase64String(Input);
                    cs.Write(xXml, 0, xXml.Length);
                }

                xBuff = ms.ToArray();
            }

            string Output = Encoding.UTF8.GetString(xBuff);
            return Output;
        }
    }
}

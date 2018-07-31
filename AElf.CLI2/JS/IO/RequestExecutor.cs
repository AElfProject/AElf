using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;

namespace AElf.CLI2.JS.IO
{
    public class RequestExecutor : IRequestExecutor
    {
        public void ExecuteAsync(string method, string url, IDictionary<string, string> headers, string body, Action<string, string> callback)
        {
            Task.Run(() =>
            {
                var resp = Execute(method, url, headers, body);
                callback(null, resp);
            });
        }

        public string Execute(string method, string url, IDictionary<string, string> headers, string body)
        {
            return Execute(new HttpMethod(method), new Uri(url), headers, body);
        }


        private static string Execute(HttpMethod method, Uri url, IDictionary<string, string> headers, string body)
        {
            if (url.IsFile)
            {
                if (method != HttpMethod.Get)
                {
                    throw new Exception("Must use GET method to read file");
                }

                using (var reader = new StreamReader(File.OpenRead(url.AbsolutePath)))
                {
                    return reader.ReadToEnd();
                } 
            }

            if (url.Scheme == "http" || url.Scheme == "https")
            {
                var client = new HttpClient {BaseAddress = new Uri(url.GetLeftPart(UriPartial.Authority))};
                foreach (var each in headers)
                {
                    client.DefaultRequestHeaders.Add(each.Key, each.Value);
                }
                return "";
            }
            else
            {
                return "";
            }
        }
    }
}
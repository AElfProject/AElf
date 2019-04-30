using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.CLI.JS.IO
{
    public class RequestExecutor : IRequestExecutor
    {
        
        public ILogger<Console> Logger { get; set; }

        public RequestExecutor()
        {
            Logger = NullLogger<Console>.Instance;
        }


        private async Task<IResponse> Execute(HttpMethod method, Uri url, IDictionary<string, string> headers, string body)
        {
            if (url.IsFile)
            {
                if (method != HttpMethod.Get)
                {
                    throw new Exception("Must use GET method to read file");
                }

                using (var reader = new StreamReader(File.OpenRead(url.AbsolutePath)))
                {
                    return new LocalFileResponse
                    {
                        Content = reader.ReadToEnd()
                    };
                } 
            }

            if (url.Scheme == "http" || url.Scheme == "https")
            {
                var client = new HttpClient {BaseAddress = new Uri(url.GetLeftPart(UriPartial.Authority))};
                if (headers != null)
                {
                    foreach (var each in headers)
                    {
                        client.DefaultRequestHeaders.Add(each.Key, each.Value);
                    }
                }

                if (method == HttpMethod.Get)
                {
                    var uriBuilder = new UriBuilder {Path = url.AbsolutePath};
                    var query1 = HttpUtility.ParseQueryString(url.Query);
                    var query2 = HttpUtility.ParseQueryString(body);
                    foreach (var q in query2.AllKeys)
                    {
                        query1[q] = query2.Get(q);
                    }

                    uriBuilder.Query = query1.ToString();
                    Logger.LogDebug(uriBuilder.Uri.ToString());
                    return new HttpResponse
                    {
                        Content = await client.GetAsync(uriBuilder.Uri)
                    };
                } 
                
                return null;
            }
            else
            {
                return null;
            }
        }

        public Task<IResponse> ExecuteAsync(string method, string url, IDictionary<string, string> headers, string body)
        {
            return Execute(new HttpMethod(method), new Uri(url), headers, body);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web;
using AElf.Common.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using NLog;

namespace AElf.CLI2.JS.IO
{
    [LoggerName("cli2.request_executor")]
    public class RequestExecutor : IRequestExecutor
    {
        private readonly ILogger _logger;

        public RequestExecutor(ILogger logger)
        {
            _logger = logger;
        }

        public void ExecuteAsync(string method, string url, IDictionary<string, string> headers, string body, Action<string, IResponse> callback)
        {
            Task.Run(() =>
            {
                var resp = Execute(method, url, headers, body);
                callback(null, resp);
            });
        }

        public IResponse Execute(string method, string url, IDictionary<string, string> headers, string body)
        {
            return Execute(new HttpMethod(method), new Uri(url), headers, body);
        }


        private IResponse Execute(HttpMethod method, Uri url, IDictionary<string, string> headers, string body)
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
                foreach (var each in headers)
                {
                    client.DefaultRequestHeaders.Add(each.Key, each.Value);
                }

                if (method == HttpMethod.Get)
                {
                    var uriBuilder = new UriBuilder();
                    uriBuilder.Path = url.AbsolutePath;
                    var query1 = HttpUtility.ParseQueryString(url.Query);
                    var query2 = HttpUtility.ParseQueryString(body);
                    foreach (var q in query2.AllKeys)
                    {
                        query1[q] = query2.Get(q);
                    }

                    uriBuilder.Query = query1.ToString();
                    _logger.Debug(uriBuilder.Uri.ToString());
                    return new HttpResponse
                    {
                        Content = client.GetAsync(uriBuilder.Uri).Result
                    };
                } 
                
                return null;
            }
            else
            {
                return null;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AElf.CLI.JS.IO
{
    public interface IResponse
    {
    }

    public class LocalFileResponse : IResponse
    {
        public string Content { get; set; }
    }

    public class HttpResponse : IResponse
    {
        public HttpResponseMessage Content { get; set; }
    }

    public interface IRequestExecutor
    {
        Task<IResponse> ExecuteAsync(
            string method,
            string url,
            IDictionary<string, string> headers,
            string body);
    }
}
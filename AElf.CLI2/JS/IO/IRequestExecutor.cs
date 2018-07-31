using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.CLI2.JS.IO
{
    public interface IRequestExecutor
    {
        void ExecuteAsync(
            string method,
            string url,
            IDictionary<string, string> headers,
            string body,
            Action<string, string> callback
        );

        string Execute(string method, string url, IDictionary<string, string> headers, string body);
    }
}
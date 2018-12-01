using System;
using ChakraCore.NET;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AElf.CLI2.JS.Net
{
      public class HttpRequestor
    {
        private HttpClient _client;
        private string _serverUrl;
        private ChakraContext _context;

        private string _toString(JSValue input)
        {
            var res = _context.GlobalObject.CallFunction<JSValue, string>("_toString", input);
            return res;
        }

        private JSValue _fromString(string input)
        {
            var res = _context.GlobalObject.CallFunction<string, JSValue>("_fromString", input);
            return res;
        }

        public HttpRequestor(string serverUrl, ChakraContext context)
        {
            _serverUrl = serverUrl;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_serverUrl);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _context = context;
        }

        public JSValue Send(JSValue payload)
        {
            var content = _toString(payload);
            var url = "/chain";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(content)
                {
                    Headers = {ContentType = MediaTypeHeaderValue.Parse("application/json")}
                }
            };
            var response = _client.SendAsync(request).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            return _fromString(result);
        }
    }
}
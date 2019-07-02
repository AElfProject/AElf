using System;
using System.Collections.Generic;
using System.Linq;
using ChakraCore.NET;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace AElf.CLI.JS.Net
{
    public class HttpRequestorError
    {
        [JsonProperty("error")] public string Error { get; } = nameof(HttpRequestorError);

        [JsonProperty("details")] public string Details { get; }

        public HttpRequestorError(string details)
        {
            Details = details;
        }
    }

    public class HttpRequestor
    {
        private HttpClient _client;
        private string _serverUrl;
        private ChakraContext _context;
        private const string ApiPath = "api";
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
            _client = new HttpClient {BaseAddress = new Uri(_serverUrl)};
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _context = context;
        }

        public JSValue Send(JSValue payload)
        {
            try
            {
                var content = _toString(payload);
                var model = JsonConvert.DeserializeObject<HttpRequestModel>(content);
                var uri = new Uri(_client.BaseAddress, $"{ApiPath}/{model.Url}");
                var request = new HttpRequestMessage(CreateHttpMethod(model.Method), uri)
                {
                    Content = new StringContent(content)
                    {
                        Headers = {ContentType = MediaTypeHeaderValue.Parse("application/json")}
                    }
                };
                using (var response = _client.SendAsync(request).Result)
                {
                    using (var httpContent = response.Content)
                    {
                        var result = httpContent.ReadAsStringAsync().Result;
                        var dic = new Dictionary<string, string> {["result"] = result};
                        var result2 = JsonConvert.SerializeObject(dic);
                        return _fromString(result2);
                    }
                }
            }
            catch (Exception e)
            {
                return _fromString(JsonConvert.SerializeObject(new HttpRequestorError(e.Message)));
            }
        }
        
        private HttpMethod CreateHttpMethod(string method)
        {
            switch (method.ToUpper())
            {
                case "POST":
                    return HttpMethod.Post;
                case "GET":
                    return HttpMethod.Get;
                case "DELETE":
                    return HttpMethod.Delete;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
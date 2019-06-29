using System;
using System.Linq;
using ChakraCore.NET;
using System.Net.Http;
using System.Net.Http.Headers;
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
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_serverUrl);
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
                var response = _client.SendAsync(request).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                return _fromString(result);
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
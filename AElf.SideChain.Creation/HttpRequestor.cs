using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AElf.SideChain.Creation
{
    public class HttpRequestor
    {
        private readonly HttpClient _client;
        private readonly string _serverUrl;
        
        public HttpRequestor(string serverUrl)
        {
            _serverUrl = serverUrl;
            _client = new HttpClient {BaseAddress = new Uri(_serverUrl)};
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string DoRequest(string endpoint, string content)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            var c = new StringContent(content);
            c.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            request.Content = c;

            string result = null;
            try
            {
                var response = _client.SendAsync(request).Result;
                result = response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception e)
            {
                ;
            }

            return result;
        }
    }
}
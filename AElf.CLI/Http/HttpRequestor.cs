using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace AElf.CLI.Http
{
    public class HttpRequestor
    {
        private HttpClient _client;
        private string _serverUrl;
        
        public HttpRequestor(string serverUrl)
        {
            _serverUrl = serverUrl;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_serverUrl);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string DoRequest(string content)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/");
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");

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
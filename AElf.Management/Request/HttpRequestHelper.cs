using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace AElf.Management.Request
{
    public class HttpRequestHelper
    {
        private static HttpClient GetClient(string serverUrl)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(serverUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.ConnectionClose = true;

            return client;
        }

        private static string DoRequest(HttpClient client, string content, string url = "")
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            var c = new StringContent(content);
            c.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            request.Content = c;

            var response = client.SendAsync(request).Result;
            var result = response.Content.ReadAsStringAsync().Result;

            return result;
        }

        public static T Request<T>(string url, object arg)
        {
            var content = JsonConvert.SerializeObject(arg);
            var client = GetClient(url);
            var result = DoRequest(client, content);

            return JsonConvert.DeserializeObject<T>(result);
        }
    }
}
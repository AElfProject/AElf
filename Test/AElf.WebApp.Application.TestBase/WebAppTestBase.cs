using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shouldly;
using Volo.Abp.AspNetCore.TestBase;
using Xunit.Abstractions;

namespace AElf.WebApp.Application
{
    public class WebAppTestBase : AbpAspNetCoreIntegratedTestBase<WebAppTestStartup> ,ITestOutputHelperAccessor
    {
        public ITestOutputHelper OutputHelper { get; set; }
        
        public WebAppTestBase(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }
        
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return base.CreateWebHostBuilder()
                .ConfigureLogging(builder =>
                {
                    builder
                        .AddXUnit(this)
                        .SetMinimumLevel(LogLevel.Information);
                });
        }

        protected async Task<T> GetResponseAsObjectAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var strResponse = await GetResponseAsStringAsync(url, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        protected async Task<string> GetResponseAsStringAsync(string url,string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await GetResponseAsync(url, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        protected async Task<HttpResponseMessage> GetResponseAsync(string url,string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
            
            var response = await Client.GetAsync(url);
            response.StatusCode.ShouldBe(expectedStatusCode);
            return response;
        }

        protected async Task<T> PostResponseAsObjectAsync<T>(string url, Dictionary<string, string> paramters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var strResponse = await PostResponseAsStringAsync(url, paramters, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        protected async Task<string> PostResponseAsStringAsync(string url, Dictionary<string, string> paramters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await PostResponseAsync(url, paramters, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        protected async Task<HttpResponseMessage> PostResponseAsync(string url, Dictionary<string,string> paramters,string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            var content = new FormUrlEncodedContent(paramters);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse($"application/x-www-form-urlencoded{version}");
            
            var response = await Client.PostAsync(url, content);
            response.StatusCode.ShouldBe(expectedStatusCode);
            return response;
        }

        protected async Task<T> DeleteResponseAsObjectAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var strResponse = await DeleteResponseAsStringAsync(url, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        protected async Task<string> DeleteResponseAsStringAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await DeleteResponseAsync(url, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        protected async Task<HttpResponseMessage> DeleteResponseAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
            
            var response = await Client.DeleteAsync(url);
            response.StatusCode.ShouldBe(expectedStatusCode);
            return response;
        }
    }
}
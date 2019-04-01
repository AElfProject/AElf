using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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

        protected async Task<T> GetResponseAsObjectAsync<T>(string url,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var strResponse = await GetResponseAsStringAsync(url, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        protected async Task<string> GetResponseAsStringAsync(string url,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await GetResponseAsync(url, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        protected async Task<HttpResponseMessage> GetResponseAsync(string url,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await Client.GetAsync(url);
            response.StatusCode.ShouldBe(expectedStatusCode);
            return response;
        }
        
        protected async Task<T> PostResponseAsObjectAsync<T>(string url, Dictionary<string,string> paramters,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var strResponse = await PostResponseAsStringAsync(url, paramters, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        protected async Task<string> PostResponseAsStringAsync(string url, Dictionary<string,string> paramters,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await PostResponseAsync(url, paramters, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        protected async Task<HttpResponseMessage> PostResponseAsync(string url, Dictionary<string,string> paramters,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var content = new FormUrlEncodedContent(paramters);
            var response = await Client.PostAsync(url, content);
            response.StatusCode.ShouldBe(expectedStatusCode);
            return response;
        }
    }
}
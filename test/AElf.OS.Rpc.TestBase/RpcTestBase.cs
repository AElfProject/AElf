using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shouldly;
using Volo.Abp.AspNetCore.TestBase;
using Xunit.Abstractions;

namespace AElf.OS.Rpc
{
    public class RpcTestBase : AbpAspNetCoreIntegratedTestBase<RpcTestStartup>, ITestOutputHelperAccessor
    {
        public ITestOutputHelper OutputHelper { get; set; }

        public RpcTestBase(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
            Client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            Client.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");
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
        
// No Get method in our rpc service, comment those methods for testing coverage.
//        protected virtual async Task<T> GetResponseAsObjectAsync<T>(string url,
//            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
//        {
//            var strResponse = await GetResponseAsStringAsync(url, expectedStatusCode);
//            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
//            {
//                ContractResolver = new CamelCasePropertyNamesContractResolver()
//            });
//        }

//        protected virtual async Task<string> GetResponseAsStringAsync(string url,
//            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
//        {
//            var response = await GetResponseAsync(url, expectedStatusCode);
//            return await response.Content.ReadAsStringAsync();
//        }

//        protected virtual async Task<HttpResponseMessage> GetResponseAsync(string url,
//            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
//        {
//            var response = await Client.GetAsync(url);
//            response.StatusCode.ShouldBe(expectedStatusCode);
//            return response;
//        }

        public async Task<HttpResponseMessage> JsonCallAsync(string path, string method, object @params = null,
            object id = null)
        {
            return await Client.PostAsync(path,
                new JsonContent(
                    JsonConvert.SerializeObject(new
                    {
                        jsonrpc = "2.0",
                        id = id ?? Guid.NewGuid(),
                        method = method,
                        @params = @params ?? new object()
                    })
                ));
        }

        public async Task<string> JsonCallAsStringAsync(string path, string method, object @params = null,
            object id = null)
        {
            var response = await JsonCallAsync(path, method, @params, id);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<JObject> JsonCallAsJObject(string path, string method, object @params = null,
            object id = null)
        {
            var response = await JsonCallAsStringAsync(path, method, @params, id);
            return JObject.Parse(response);
        }
    }

    public class JsonContent : StringContent
    {
        public JsonContent(string content) : this(content, null, null)
        {
        }

        // ReSharper disable once IntroduceOptionalParameters.Global
//        protected JsonContent(string content, Encoding encoding) : this(content, encoding, null)
//        {
//        }

        protected JsonContent(string content, Encoding encoding, string mediaType) : base(content, encoding, mediaType)
        {
            this.Headers.ContentType = new MediaTypeHeaderValue(mediaType ?? "application/json")
            {
                CharSet = null
            };
        }
    }
}
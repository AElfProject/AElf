using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.WebApp.Application.Chain.Tests
{
    public class VersionTestAppServiceTest : WebAppTestBase
    {
        public VersionTestAppServiceTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task GetTest()
        {
            var v1 = "1.0";
            var v1Response = await GetResponseAsStringAsync("/api/versionTest/test", v1);
            v1Response.ShouldBe($"Get Test: v{v1}");

            var v2 = "2.0";
            var v2Response = await GetResponseAsStringAsync("/api/versionTest/test", v2);
            v2Response.ShouldBe($"Get Test: v{v2}");

            var v3 = "3.0";
            var v3Response = await GetResponseAsStringAsync("/api/versionTest/test", v3);
            v3Response.ShouldBe($"Get Test: v{v3}");
            
            //Not exist version
            var v4 = "4.0";
            var v4Response =
                await GetResponseAsObjectAsync<WebAppErrorResponse>("/api/versionTest/test", v4, HttpStatusCode.BadRequest);
            v4Response.Error.Code.ShouldBe("UnsupportedApiVersion");
            v4Response.Error.Message.ShouldBe($"The HTTP resource that matches the request URI 'http://localhost/api/versionTest/test' does not support the API version '{v4}'.");
            
            //Use latest version default
            var response = await GetResponseAsStringAsync("/api/versionTest/test");
            response.Trim('\"').ShouldBe($"Get Test: v{v3}");
            
        }

        [Fact]
        public async Task PostTest()
        {
            var v1 = "1.0";
            var test = "test";
            var paramters = new Dictionary<string, string>
            {
                {"test", test}
            };
            var v1Response = await PostResponseAsStringAsync("/api/versionTest/test", paramters, v1);
            v1Response.ShouldBe($"Post Test v{v1}: {test}");

            var v2 = "2.0";
            var v2Response = await PostResponseAsStringAsync("/api/versionTest/test", paramters, v2);
            v2Response.ShouldBe($"Post Test v{v2}: {test}");

            var v3 = "3.0";
            var v3Response = await PostResponseAsStringAsync("/api/versionTest/test", paramters, v3);
            v3Response.ShouldBe($"Post Test v{v3}: {test}");
            
            //Not exist version
            var v4 = "4.0";
            var v4Response = await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/versionTest/test",
                paramters, v4, HttpStatusCode.BadRequest);
            v4Response.Error.Code.ShouldBe("UnsupportedApiVersion");
            v4Response.Error.Message.ShouldBe($"The HTTP resource that matches the request URI 'http://localhost/api/versionTest/test' does not support the API version '{v4}'.");
            
            //Use latest version default
            var response = await PostResponseAsStringAsync("/api/versionTest/test", paramters, v3);
            response.ShouldBe($"Post Test v{v3}: {test}");
        }
        
        [Fact]
        public async Task DeleteTest()
        {
            var v1 = "1.0";
            var test = "test";
            var v1Response = await DeleteResponseAsStringAsync($"/api/versionTest/test?test={test}", v1);
            v1Response.ShouldBe($"Delete Test v{v1}: {test}");

            var v2 = "2.0";
            var v2Response = await DeleteResponseAsStringAsync($"/api/versionTest/test?test={test}", v2);
            v2Response.ShouldBe($"Delete Test v{v2}: {test}");

            var v3 = "3.0";
            var v3Response = await DeleteResponseAsStringAsync($"/api/versionTest/test?test={test}", v3);
            v3Response.ShouldBe($"Delete Test v{v3}: {test}");
            
            //Not exist version
            var v4 = "4.0";
            var v4Response =
                await DeleteResponseAsObjectAsync<WebAppErrorResponse>($"/api/versionTest/test?test={test}", v4,
                    HttpStatusCode.BadRequest);
            v4Response.Error.Code.ShouldBe("UnsupportedApiVersion");
            v4Response.Error.Message.ShouldBe($"The HTTP resource that matches the request URI 'http://localhost/api/versionTest/test' does not support the API version '{v4}'.");
            
            //Use latest version default
            var response = await DeleteResponseAsStringAsync($"/api/versionTest/test?test={test}");
            response.Trim('\"').ShouldBe($"Delete Test v{v3}: {test}");
        }
    }
}
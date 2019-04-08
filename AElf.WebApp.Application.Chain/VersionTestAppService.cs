using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IVersionTestAppService :IApplicationService
    {
        string GetTest();

        string PostTest(string test);

        string PostTest_V2(string test);

        string PostTest_V3(string test);
    }
    
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
    public class VersionTestAppService : IVersionTestAppService
    {
        private readonly IHttpContextAccessor _httpContext;

        public VersionTestAppService(IHttpContextAccessor httpContext)
        {
            _httpContext = httpContext;
        }
        
        public string GetTest()
        {
            return $"Get Test: v{_httpContext.HttpContext.GetRequestedApiVersion().ToString()}";
        }

        [MapToApiVersion("1.0")]
        public string PostTest(string test)
        {
            return $"Post Test v{_httpContext.HttpContext.GetRequestedApiVersion().ToString()}: {test}";
        }
        
        [ActionName("Test")]
        [MapToApiVersion("2.0")]
        [Obsolete]
        public string PostTest_V2(string test)
        {
            return $"Post Test v{_httpContext.HttpContext.GetRequestedApiVersion().ToString()}: {test}";
        }
        
        [ActionName("Test")]
        [MapToApiVersion("3.0")]
        public string PostTest_V3(string test)
        {
            return $"Post Test v{_httpContext.HttpContext.GetRequestedApiVersion().ToString()}: {test}";
        }
        
        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        public string DeleteTest(string test)
        {
            return $"Delete Test v{_httpContext.HttpContext.GetRequestedApiVersion().ToString()}: {test}";
        }
        
        [ActionName("DeleteTest")]
        [MapToApiVersion("3.0")]
        public string DeleteTest_V3(string test)
        {
            return $"Delete Test v{_httpContext.HttpContext.GetRequestedApiVersion().ToString()}: {test}";
        }
    }
}
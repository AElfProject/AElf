using System;
using AElf.Management.Models;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SideChainController : ControllerBase
    {
        private readonly ISideChainService _sideChainService;

        public SideChainController(ISideChainService sideChainService)
        {
            _sideChainService = sideChainService;
        }

        [HttpGet("{value}")]
        public ApiResult Test(string value)
        {
            return new TestResult {Value = value};
        }

        [HttpPost("{chainId}")]
        public ApiResult Post(string chainId, [FromBody] DeployArg arg)
        {
            _sideChainService.Deploy(chainId, arg);
            return new ApiResult();
        }

        [HttpDelete("{chainId}")]
        public ApiResult Delete(string chainId)
        {
            _sideChainService.Remove(chainId);
            return new ApiResult();
        }
    }
}
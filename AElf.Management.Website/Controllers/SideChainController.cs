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

        [HttpPost("{chainId}")]
        public ApiEmptyResult Post(string chainId, [FromBody] DeployArg arg)
        {
            _sideChainService.Deploy(chainId, arg);
            return ApiEmptyResult.Default;
        }

        [HttpDelete("{chainId}")]
        public ApiEmptyResult Delete(string chainId)
        {
            _sideChainService.Remove(chainId);
            return ApiEmptyResult.Default;
        }
    }
}
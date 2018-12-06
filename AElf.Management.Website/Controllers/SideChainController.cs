using System;
using System.Threading.Tasks;
using AElf.Configuration;
using AElf.Management.Interfaces;
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
        public async Task<ApiEmptyResult> Post(string chainId, [FromBody] DeployArg arg)
        {
            Console.WriteLine(JsonSerializer.Instance.Serialize(arg));
            arg.MainChainId = chainId;
            _sideChainService.Deploy(arg);
            return ApiEmptyResult.Default;
        }

        [HttpDelete("{chainId}")]
        public async Task<ApiEmptyResult> Delete(string chainId)
        {
            _sideChainService.Remove(chainId);
            return ApiEmptyResult.Default;
        }
    }
}
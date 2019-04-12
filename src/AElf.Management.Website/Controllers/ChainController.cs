using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChainController : ControllerBase
    {
        private readonly IChainService _chainService;

        public ChainController(IChainService chainService)
        {
            _chainService = chainService;
        }

        [HttpGet]
        [Route("list")]
        public async Task<ApiResult<List<ChainResult>>> List()
        {
            var result = await _chainService.GetAllChains();
            return new ApiResult<List<ChainResult>>(result);
        }

        [HttpPost]
        public async Task<ApiEmptyResult> Deploy([FromBody] DeployArg arg)
        {
            await _chainService.DeployMainChain(arg);
            return ApiEmptyResult.Default;
        }

        [HttpDelete]
        [Route("{chainId}")]
        public async Task<ApiEmptyResult> Remove(string chainId)
        {
            await _chainService.RemoveMainChain(chainId);
            return ApiEmptyResult.Default;
        }
    }
}
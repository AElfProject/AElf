using System.Threading.Tasks;
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
            arg.MainChainId = chainId;
            await _sideChainService.Deploy(arg);
            return ApiEmptyResult.Default;
        }

        [HttpDelete("{chainId}")]
        public async Task<ApiEmptyResult> Delete(string chainId)
        {
            await _sideChainService.Remove(chainId);
            return ApiEmptyResult.Default;
        }
    }
}
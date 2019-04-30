using System.Threading.Tasks;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NetworkController : ControllerBase
    {
        private readonly INetworkService _networkService;

        public NetworkController(INetworkService networkService)
        {
            _networkService = networkService;
        }

        [HttpGet]
        [Route("peers/{chainId}")]
        public async Task<ApiResult<PeerResult>> Peers(string chainId)
        {
            var result = await _networkService.GetPeers(chainId);

            return new ApiResult<PeerResult>(result);
        }

//        [HttpGet]
//        [Route("pool/{chainId}")]
//        public ApiResult<PoolStateResult> PoolState(string chainId)
//        {
//            var result = _networkService.GetPoolState(chainId);
//            
//            return new ApiResult<PoolStateResult>(result);
//        }
//        
//        [HttpGet]
//        [Route("poolstatehistory/{chainId}")]
//        public ApiResult<List<PoolStateHistory>> StateHistory(string chainId)
//        {
//            var result = _networkService.GetPoolStateHistory(chainId);
//            
//            return new ApiResult<List<PoolStateHistory>>(result);
//        }
    }
}
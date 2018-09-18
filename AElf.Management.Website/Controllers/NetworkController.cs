using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    public class NetworkController: ControllerBase
    {
        private readonly INetworkService _networkService;

        public NetworkController(INetworkService networkService)
        {
            _networkService = networkService;
        }
        
        [HttpGet]
        [Route("peers/{chainId}")]
        public ApiResult<PeerResult> Peers(string chainId)
        {
            var result = _networkService.GetPeers(chainId);
            
            return new ApiResult<PeerResult>(result);
        }
        
        [HttpGet]
        [Route("pool/{chainId}")]
        public ApiResult<PoolStateResult> PoolState(string chainId)
        {
            var result = _networkService.GetPoolState(chainId);
            
            return new ApiResult<PoolStateResult>(result);
        }
    }
}
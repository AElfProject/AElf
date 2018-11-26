using System.Collections.Generic;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NodeController: ControllerBase
    {
        private readonly INodeService _nodeService;

        public NodeController(INodeService nodeService)
        {
            _nodeService = nodeService;
        }

        [HttpGet]
        [Route("isalive/{chainId}")]
        public ApiResult<bool> IsAlive(string chainId)
        {
            var result = _nodeService.IsAlive(chainId);
            
            return new ApiResult<bool>(result);
        }
        
        [HttpGet]
        [Route("isforked/{chainId}")]
        public ApiResult<bool> IsForked(string chainId)
        {
            var result = _nodeService.IsForked(chainId);
            
            return new ApiResult<bool>(result);
        }
        
        [HttpGet]
        [Route("statehistory/{chainId}")]
        public ApiResult<List<NodeStateHistory>> StateHistory(string chainId)
        {
            var result = _nodeService.GetHistoryState(chainId);
            
            return new ApiResult<List<NodeStateHistory>>(result);
        }
    }
}
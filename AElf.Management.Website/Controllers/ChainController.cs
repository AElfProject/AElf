using System;
using System.Collections.Generic;
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
        public ApiResult<List<ChainResult>> List()
        {
            var result = _chainService.GetAllChains();
            return new ApiResult<List<ChainResult>>(result);
        }

        [HttpPost]
        public ApiEmptyResult Deploy([FromBody] DeployArg arg)
        {
            _chainService.DeployMainChain(arg.MainChainId, arg);
            return ApiEmptyResult.Default;
        }
        
        [HttpPost]
        [Route("test")]
        public ApiResult<DeployTestChainResult> DeployTestChain()
        {
            var result = _chainService.DeployTestChain();
            return new ApiResult<DeployTestChainResult>(result);
        }
        
        [HttpDelete]
        [Route("{chain}")]
        public ApiEmptyResult Remove(string chainId)
        {
            _chainService.RemoveMainChain(chainId);
            return ApiEmptyResult.Default;
        }
        
        [HttpDelete]
        [Route("test/{chainId}")]
        public ApiEmptyResult RemoveTestChain(string chainId)
        {
            _chainService.RemoveTestChain(chainId);
            return ApiEmptyResult.Default;
        }
    }
}
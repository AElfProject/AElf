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
    }
}
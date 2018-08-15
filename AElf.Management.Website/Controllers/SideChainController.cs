using System;
using AElf.Management.Models;
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
        public string Test(string value)
        {
            return "reciced:" + value;
        }

        [HttpPost("{chainId}")]
        public void Post(string chainId, [FromBody] DeployArg arg)
        {
            _sideChainService.Deploy(chainId, arg);
        }

        [HttpDelete("{chainId}")]
        public void Delete(string chainId)
        {
            _sideChainService.Remove(chainId);
        }
    }
}
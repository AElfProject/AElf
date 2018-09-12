using System;
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

        [HttpPost]
        public ApiEmptyResult Post([FromBody] DeployArg arg)
        {
            Console.WriteLine(JsonSerializer.Instance.Serialize(arg));
            _sideChainService.Deploy(arg);
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
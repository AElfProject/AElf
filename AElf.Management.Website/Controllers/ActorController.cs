using System.Collections.Generic;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActorController : ControllerBase
    {
        private readonly IAkkaService _akkaService;

        public ActorController(IAkkaService akkaService)
        {
            _akkaService = akkaService;
        }
        
        [HttpGet]
        [Route("state/{chainId}")]
        public ApiResult<List<ActorStateResult>> State(string chainId)
        {
            var result = _akkaService.GetState(chainId);
            return new ApiResult<List<ActorStateResult>>(result);
        }
    }
}
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
        private readonly IActorService _actorService;

        public ActorController(IActorService actorService)
        {
            _actorService = actorService;
        }
        
        [HttpGet]
        [Route("state/{chainId}")]
        public ApiResult<List<MemberInfo>> State(string chainId)
        {
            var result = _actorService.GetState(chainId);
            return new ApiResult<List<MemberInfo>>(result);
        }
    }
}
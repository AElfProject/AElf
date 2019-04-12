using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LighthouseController : ControllerBase
    {
        private readonly ILighthouseService _lighthouseService;

        public LighthouseController(ILighthouseService lighthouseService)
        {
            _lighthouseService = lighthouseService;
        }

        [HttpGet]
        [Route("list/{chainId}")]
        public async Task<ApiResult<List<LighthouseResult>>> List(string chainId)
        {
            var result = await _lighthouseService.GetAllLighthouses(chainId);
            return new ApiResult<List<LighthouseResult>>(result);
        }
    }
}
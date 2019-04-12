using System.Threading.Tasks;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet("{value}")]
        public async Task<ApiResult<string>> Test(string value)
        {
            return await Task.FromResult(new ApiResult<string>(value));
        }
    }
}
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet("{value}")]
        public ApiResult<string> Test(string value)
        {
            return new ApiResult<string>(value);
        }
    }
}
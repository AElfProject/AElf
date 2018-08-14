using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SideChainController : ControllerBase
    {
        // POST api/sidechain
        [HttpPost("{chainId}")]
        public void Post(string chainId,[FromBody] TestModel value)
        {
            Console.WriteLine(chainId);
            Console.WriteLine(value.Value);
        }

        // DELETE api/sidechain/abcde
        [HttpDelete("{chainId}")]
        public void Delete(string chainId)
        {
            Console.WriteLine(chainId);
        }
    }

    public class TestModel
    {
        public string Value { get; set; }
    }
}
using System.Collections.Generic;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        private readonly IManagerService _managerService;

        public ManagerController(IManagerService managerService)
        {
            _managerService = managerService;
        }

        [HttpGet]
        [Route("list/{chianId}")]
        public ApiResult<List<ManagerResult>> List(string chianId)
        {
            var result = _managerService.GetAllManagers(chianId);
            return new ApiResult<List<ManagerResult>>(result);
        }
    }
}
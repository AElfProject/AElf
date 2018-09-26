using System.Collections.Generic;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Management.Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        
        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }
        
        [HttpGet]
        [Route("poolsize/{chainId}")]
        public ApiResult<ulong> PoolSize(string chainId)
        {
            var result = _transactionService.GetPoolSize(chainId);
            return new ApiResult<ulong>(result);
        }
        
        [HttpGet]
        [Route("poolsizehistory/{chainId}")]
        public ApiResult<List<PoolSizeHistory>> PoolSizeHistory(string chainId)
        {
            var result = _transactionService.GetPoolSizeHistory(chainId);
            
            return new ApiResult<List<PoolSizeHistory>>(result);
        }
    }
}
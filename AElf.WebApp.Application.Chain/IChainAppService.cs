using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IInfoAppService : IApplicationService
    {
        Task<AElf.Kernel.Chain> GetInfoAsync();
    }

    public class InfoAppService : IInfoAppService
    {
        private readonly IBlockchainService _blockchainService;

        public InfoAppService(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        [ActionName("")]
        [HttpGet]
        public async Task<AElf.Kernel.Chain> GetInfoAsync()
        {
            return await _blockchainService.GetChainAsync();
        }
    }
}
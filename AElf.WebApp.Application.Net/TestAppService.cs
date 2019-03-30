using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network.Application;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Net
{
    public interface IPeerAppService : IApplicationService
    {
        Task<Transaction> GetTransactionByAddressAsync(Address address);
        Task<PeerTestDto> UpdateOneAsync(PeerTestDto input);

        Task<PeerTestDto> ThrowErrorAsync();

        Task<List<string>> GetPeers();
    }

    public class PeerTestDto
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class PeerAppService : IPeerAppService
    {
        private INetworkService _networkService;

        public PeerAppService(INetworkService networkService)
        {
            _networkService = networkService;
        }

        public async Task<Transaction> GetTransactionByAddressAsync(Address address)
        {
            return new Transaction() {To = address};
        }
        
        public async Task<Transaction> GetRandomTransactionAsync()
        {
            return new Transaction() {To = Address.Generate()};
        }

        [HttpPost]
        public async Task<PeerTestDto> UpdateOneAsync(PeerTestDto input)
        {
            return input;
        }

        public async Task<PeerTestDto> ThrowErrorAsync()
        {
            throw new UserFriendlyException("hello","1002");
        }

        public async Task<List<string>> GetPeers()
        {
            return _networkService.GetPeers();
        }
    }
}
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
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
    }

    public class PeerTestDto
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class PeerAppService : IPeerAppService
    {
        public async Task<Transaction> GetTransactionByAddressAsync(Address address)
        {
            return new Transaction() {To = address};
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
    }
}
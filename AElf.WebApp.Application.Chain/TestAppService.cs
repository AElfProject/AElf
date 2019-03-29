using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Kernel
{
    public interface ITest2AppService : IApplicationService
    {
        Task<Transaction> GetTransactionByAddressAsync(Address address);
        Task<TestDto> UpdateOneAsync(TestDto input);

        Task<TestDto> ThrowErrorAsync();
    }

    public class TestDto
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class Test2AppService : ITest2AppService
    {
        public async Task<Transaction> GetTransactionByAddressAsync(Address address)
        {
            return new Transaction() {To = address};
        }

        [HttpPost]
        public async Task<TestDto> UpdateOneAsync(TestDto input)
        {
            return input;
        }

        public async Task<TestDto> ThrowErrorAsync()
        {
            throw new UserFriendlyException("hello","1002");
        }
    }
}
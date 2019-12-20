using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractExecutiveService : ISmartContractExecutiveService, ISingletonDependency
    {
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;

        public ILogger<SmartContractExecutiveService> Logger { get; set; }

        public SmartContractExecutiveService(ISmartContractExecutiveProvider smartContractExecutiveProvider)
        {
            _smartContractExecutiveProvider = smartContractExecutiveProvider;

            Logger = NullLogger<SmartContractExecutiveService>.Instance;
        }

        public async Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address)
        {
            return await _smartContractExecutiveProvider.GetExecutiveAsync(chainContext, address);
        }

        public virtual async Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            await _smartContractExecutiveProvider.PutExecutiveAsync(address, executive);
        }

        public void CleanIdleExecutive()
        {
            _smartContractExecutiveProvider.CleanIdleExecutive();
        }
    }
}
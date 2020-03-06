using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    /// <summary>
    /// a smart contract executive, don't use it out of AElf.Kernel.SmartContract
    /// </summary>
    public interface ISmartContractExecutiveService
    {
        Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address);

        Task PutExecutiveAsync(Address address, IExecutive executive);

        Task<SmartContractRegistration> GetSmartContractRegistrationAsync(
            IChainContext chainContext, Address address);

        void CleanIdleExecutive();

        void AddContractInfo(Address address, long blockHeight);

        void ClearContractInfo(long height);
    }
}
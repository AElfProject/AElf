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

        //TODO:This method has no relation with Executive, move out. You break an OOD rules, don't make a big class, one
        //class do one thing. https://www.cnblogs.com/oomusou/archive/2007/05/05/736658.html #8, #27
        void AddContractInfo(Address address, long blockHeight);

        //TODO:This method has no relation with Executive, move out
        void ClearContractInfo(long height);
    }
}
using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.FeeCalculation.Application
{
    // TODO: Remove this interface after new fee charging solution adopted.
    public interface IChargeFeeStrategy
    {
        Address GetContractAddress(IChainContext chainContext);
        string MethodName { get; }
        bool IsFree(Transaction transaction);
    }

    public abstract class ChargeFeeStrategyBase : IChargeFeeStrategy
    {
        protected readonly ISmartContractAddressService SmartContractAddressService;

        protected ChargeFeeStrategyBase(ISmartContractAddressService smartContractAddressService)
        {
            SmartContractAddressService = smartContractAddressService;
        }

        public abstract Address GetContractAddress(IChainContext chainContext);

        public abstract string MethodName { get; }

        public bool IsFree(Transaction transaction)
        {
            return GetInvolvedSmartContractMethods().Contains(transaction.MethodName);
        }

        protected abstract List<string> GetInvolvedSmartContractMethods();
    }
}
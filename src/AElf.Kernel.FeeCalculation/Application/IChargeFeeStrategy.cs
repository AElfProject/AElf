using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.FeeCalculation.Application
{
    public interface IChargeFeeStrategy
    {
        Address ContractAddress { get; }
        string MethodName { get; }
        bool IsFree(Transaction transaction);
    }

    public abstract class ChargeFeeStrategyBase : IChargeFeeStrategy
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        protected ChargeFeeStrategyBase(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Address ContractAddress => _smartContractAddressService.GetAddressByContractName(SystemContractHashName);

        public abstract string MethodName { get; }

        public bool IsFree(Transaction transaction)
        {
            return GetInvolvedSmartContractMethods().Contains(transaction.MethodName);
        }

        protected abstract Hash SystemContractHashName { get; }

        protected abstract List<string> GetInvolvedSmartContractMethods();
    }
}
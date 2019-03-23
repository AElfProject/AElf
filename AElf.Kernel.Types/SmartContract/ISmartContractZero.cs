using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Types;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        ContractInfo GetContractInfo(Address address);
        Address DeploySmartContract(ContractDeploymentInput input);

        Address DeploySystemSmartContract(SystemContractDeploymentInput input);

        Address GetContractAddressByName(Hash name);
        SmartContractRegistration GetSmartContractRegistrationByAddress(Address address);
    }
}
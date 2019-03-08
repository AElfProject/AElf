using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Types;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        string GetContractInfo(Address address);
        Address DeploySmartContract(int category, byte[] code);
        Address DeploySystemSmartContract(Hash name, int category, byte[] code);

        Address GetContractAddressByName(Hash name);
        SmartContractRegistration GetSmartContractRegistrationByAddress(Address address);
    }
}
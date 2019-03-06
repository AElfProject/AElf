using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Types;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        string GetContractInfo(Address address);
        byte[] DeploySmartContract(int category, byte[] code);
        byte[] DeploySystemSmartContract(Hash name, int category, byte[] code);

        Address GetContractAddressByCodeHash(Hash codeHash);

        Address GetContractAddressByName(Hash name);
        SmartContractRegistration GetSmartContractRegistrationByAddress(Address address);
    }
}
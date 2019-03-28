using System;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface ISmartContractRunner
    {
        int Category { get; }
        Task<IExecutive> RunAsync(SmartContractRegistration reg);
        void CodeCheck(byte[] code, bool isPrivileged = false);
        ContractMetadataTemplate ExtractMetadata(Type contractType);
    }
}
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface ISmartContractRunner
    {
        int Category { get; }
        Task<IExecutive> RunAsync(SmartContractRegistration reg);
        void CodeCheck(byte[] code, bool isPrivileged = false);
        ContractMetadataTemplate ExtractMetadata(System.Type contractType);
    }
}
using System.Threading.Tasks;
using Google.Protobuf;
using AElf.Kernel;

namespace AElf.Kernel.SmartContract
{
    public interface ISmartContractRunner
    {
        int Category { get; }
        Task<IExecutive> RunAsync(SmartContractRegistration reg);
        IMessage GetAbi(SmartContractRegistration reg);
        System.Type GetContractType(SmartContractRegistration reg);
        void CodeCheck(byte[] code, bool isPrivileged = false);
        ContractMetadataTemplate ExtractMetadata(System.Type contractType);
    }
}
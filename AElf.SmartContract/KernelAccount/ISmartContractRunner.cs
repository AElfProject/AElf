using System.Threading.Tasks;
using Google.Protobuf;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface ISmartContractRunner
    {
        Task<IExecutive> RunAsync(SmartContractRegistration reg);
        IMessage GetAbi(SmartContractRegistration reg, string name = null);
        System.Type GetContractType(SmartContractRegistration reg);
        void CodeCheck(byte[] code, bool isPrivileged=false);
        ContractMetadataTemplate ExtractMetadata(System.Type contractType);
    }
}
using AElf.Kernel.SmartContract;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly;

public interface IExternalEnvironment
{
    IHostSmartContractBridgeContext? HostSmartContractBridgeContext { get; set; }
    Dictionary<string, ByteString> Writes { get; set; }
    Dictionary<string, bool> Reads { get; set; }
    Dictionary<string, bool> Deletes { get; set; }
    WriteOutcome SetStorage(byte[] key, byte[] value, bool takeOld);
    Task<byte[]?> GetStorageAsync(byte[] key);
    void SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext);
}
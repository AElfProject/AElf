using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.Contract;

public interface IWebAssemblySmartContractContext
{
    byte[] CallMethod(Address fromAddress, Address toAddress, string methodName, ByteString args);
    byte[] DelegateCall(Address fromAddress, Address toAddress, string methodName, ByteString args);
}
using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly;

public interface IWebAssemblyChainExtension
{
    byte[] CallMethod(Address fromAddress, Address toAddress, string methodName, ByteString args);
    byte[] DelegateCall(Address fromAddress, Address toAddress, string methodName, ByteString args);
}
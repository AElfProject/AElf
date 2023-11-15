using Google.Protobuf;
using Nethereum.ABI;

namespace AElf.Runtime.WebAssembly.Types;

public static class WebAssemblyTypeHelper
{
    public static ByteString ConvertToParameter(params ABIValue[] abiValues)
    {
        return ByteString.CopyFrom(new ABIEncode().GetABIEncoded(abiValues));
    }
}
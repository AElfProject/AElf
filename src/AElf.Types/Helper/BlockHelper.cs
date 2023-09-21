using System.Linq;
using AElf.Types;
using Google.Protobuf;

namespace AElf
{

    public static class BlockHelper
    {
        public static ByteString GetRefBlockPrefix(Hash blockHash)
        {
            var refBlockPrefix = ByteString.CopyFrom(blockHash.Value.Take(4).ToArray());
            return refBlockPrefix;
        }
    }
}
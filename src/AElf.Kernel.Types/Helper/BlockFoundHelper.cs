using System.Linq;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public static class BlockFoundHelper
    {
        public static ByteString GetPreBlock(Hash blockHash)
        {
            var refBlockPrefix = ByteString.CopyFrom(blockHash.Value.Take(4).ToArray());
            return refBlockPrefix;
        }
    }
}
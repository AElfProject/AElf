using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist.Extensions
{
    public static class WhitelistExtensions
    {
        public static Hash CalculateExtraInfoId(this ByteString extraInfo)
        {
            return HashHelper.ComputeFrom(extraInfo.ToByteArray());
        }
    }
}
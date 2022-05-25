using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist.Extensions
{
    public static class WhitelistExtensions
    {
        public static Hash CalculateExtraInfoId(this Address sender,Hash projectId,string tagName)
        {
            return HashHelper.ComputeFrom($"{sender}{projectId}{tagName}");
        }
    }
}
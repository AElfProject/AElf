using Acs3;
using AElf.Types;

namespace AElf.Contracts.Referendum
{
    public static class ProposerWhiteListExtensions
    {
        public static int Count(this ProposerWhiteList proposerWhiteList)
        {
            return proposerWhiteList.Proposers.Count;
        }

        public static bool Empty(this ProposerWhiteList proposerWhiteList)
        {
            return proposerWhiteList.Count() == 0;
        }

        public static bool Contains(this ProposerWhiteList proposerWhiteList, Address address)
        {
            return proposerWhiteList.Proposers.Contains(address);
        }
    }
}
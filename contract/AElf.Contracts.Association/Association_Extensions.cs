using System.Linq;
using AElf.Standards.ACS3;
using AElf.Types;

namespace AElf.Contracts.Association
{
    public static class AssociationExtensions
    {
        public static int Count(this ProposerWhiteList proposerWhiteList)
        {
            return proposerWhiteList.Proposers.Count;
        }

        public static bool Empty(this ProposerWhiteList proposerWhiteList)
        {
            return proposerWhiteList.Count() == 0;
        }

        public static bool AnyDuplicate(this ProposerWhiteList proposerWhiteList)
        {
            return proposerWhiteList.Proposers.GroupBy(p => p).Any(g => g.Count() > 1);
        }

        public static bool AnyDuplicate(this OrganizationMemberList organizationMemberList)
        {
            return organizationMemberList.OrganizationMembers.GroupBy(m => m).Any(g => g.Count() > 1);
        }

        public static bool Contains(this ProposerWhiteList proposerWhiteList, Address address)
        {
            return proposerWhiteList.Proposers.Contains(address);
        }
    }
}
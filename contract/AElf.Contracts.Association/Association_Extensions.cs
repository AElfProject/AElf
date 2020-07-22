using Acs3;
using AElf.Types;

namespace AElf.Contracts.Association
{
    public static class AssociationExtensions
    {
        public static int Count(this ProposerWhiteList proposerWhiteList)
        {
            return proposerWhiteList.Proposers.Count;
        }

        public static int Count(this OrganizationMemberList organizationMemberList)
        {
            return organizationMemberList.OrganizationMembers.Count;
        }
        
        public static bool Empty(this ProposerWhiteList proposerWhiteList)
        {
            return proposerWhiteList.Count() == 0;
        }
        
        public static bool Empty(this OrganizationMemberList organizationMemberList)
        {
            return organizationMemberList.Count() == 0;
        }

        public static bool Contains(this ProposerWhiteList proposerWhiteList, Address address)
        {
            return proposerWhiteList.Proposers.Contains(address);
        }
        
        public static bool Contains(this OrganizationMemberList organizationMemberList, Address address)
        {
            return organizationMemberList.OrganizationMembers.Contains(address);
        }
    }
}
using AElf.Types;

namespace AElf.Contracts.Association
{
    public partial class OrganizationMemberList
    {
        public int Count()
        {
            return organizationMembers_.Count;
        }

        public bool Empty()
        {
            return Count() == 0;
        }

        public bool Contains(Address address)
        {
            return organizationMembers_.Contains(address);
        }
    }
}
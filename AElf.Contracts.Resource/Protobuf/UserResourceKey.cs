using AElf.Common;

namespace AElf.Contracts.Resource
{
    public partial class UserResourceKey
    {
        public UserResourceKey(Address userAddress, ResourceType resourceType)
        {
            Address = userAddress;
            Type = resourceType;
        }
    }
}
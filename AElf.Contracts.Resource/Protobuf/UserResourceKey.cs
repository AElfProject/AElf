using AElf.Common;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Resource
{
    public partial class UserResourceKey
    {
        public UserResourceKey(Address userAddress, ResourceType resourceType)
        {
            this.Address = userAddress;
            this.Type = resourceType;
        }
    }
}
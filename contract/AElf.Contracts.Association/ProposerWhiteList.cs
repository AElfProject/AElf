using AElf.Types;

namespace AElf.Contracts.Association
{
    public partial class ProposerWhiteList
    {
        public int Count()
        {
            return proposers_.Count;
        }
        
        public bool Empty()
        {
            return Count() == 0;
        }

        public bool Contains(Address address)
        {
            return proposers_.Contains(address);
        }
    }
}
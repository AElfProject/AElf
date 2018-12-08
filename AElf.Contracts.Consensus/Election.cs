using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus
{
    public class Election
    {
        
        private readonly DataCollection _collection;

        public Election(DataCollection collection)
        {
            _collection = collection;
        }

        public void AnnounceElection()
        {
            Api.LockToken(GlobalConfig.LockTokenForElection);
        }

        public void QuitElection()
        {
            
        }

        public void Vote()
        {
            
        }

        public void Renew()
        {
            
        }
    }
}
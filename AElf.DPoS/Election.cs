using System;
using System.Text;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp.Types;

namespace AElf.DPoS
{
    public class Election
    {
        public Map GoLivePoolMembers = new Map("GoLivePoolMembers");
        
        public Map Votes = new Map("Votes");

        public async Task<object> RegisterToCampaign(Hash accountHash, string alias)
        {
            await GoLivePoolMembers.SetValueAsync(accountHash, Encoding.UTF8.GetBytes(alias));
            await Votes.SetValueAsync(alias.CalculateHash(), ((ulong) 0).ToBytes());
            return null;
        }
        
        
    }
}
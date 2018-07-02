﻿using System;
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
        
        //Potential mining nodes' votes.
        public Map Votes = new Map("Votes");
        
        //Remain votes of voters
        public Map RemainVotes = new Map("RemainVotes");
        
        // TODO: Should use another smart contract to get ELF token balace.
        public Map PreviousTermBalance = new Map("PreviousTermBalance");

        public async Task<object> InitializeAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<object> RegisterToCampaign(Hash accountHash, string alias)
        {
            await GoLivePoolMembers.SetValueAsync(accountHash, Encoding.UTF8.GetBytes(alias));
            await Votes.SetValueAsync(alias.CalculateHash(), ((ulong) 0).ToBytes());
            return null;
        }
        
        public async Task<object> Vote(Hash voterAddress, string alias, ulong votes)
        {
            if ((ulong)await GetRemainVotes(voterAddress) < votes)
            {
                return false;
            }

            var currentVotes = (await Votes.GetValue(alias.CalculateHash())).ToUInt64();
            await Votes.SetValueAsync(alias.CalculateHash(), (currentVotes + votes).ToBytes());
            return true;
        }

        public async Task<object> GetRemainVotes(Hash voterAddress)
        {
            return await RemainVotes.GetValue(voterAddress);
        }

        public async Task<object> InitialVoting(Hash voterAddress)
        {
            var balance = (await PreviousTermBalance.GetValue(voterAddress)).ToUInt64();
            await RemainVotes.SetValueAsync(voterAddress, (balance * 30).ToBytes());
            return true;
        }

    }
}
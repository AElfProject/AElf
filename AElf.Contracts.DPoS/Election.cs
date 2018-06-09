using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using ServiceStack;

namespace AElf.Contracts.DPoS
{
    public class Election : CSharpSmartContract
    {
        public Map GoLivePoolMembers = new Map("GoLivePoolMembers");
        
        //Potential mining nodes' votes.
        public Map Votes = new Map("Votes");
        
        //Remain votes of voters
        public Map RemainVotes = new Map("RemainVotes");
        
        public override async Task InvokeAsync()
        {
            var tx = Api.GetTransaction();

            var methodname = tx.MethodName;
            var type = GetType();
            var member = type.GetMethod(methodname);
            // params array
            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();

            // invoke
            await (Task<object>) member.Invoke(this, parameters);
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
    }
}
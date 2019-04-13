using System.Collections.Generic;
using System.Linq;
using AElf.Consensus.DPoS;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class ConsensusContract
    {
        private void SetInitialMinersAliases(IEnumerable<string> publicKeys)
        {
            var index = 0;
            var aliases = DPoSContractConsts.InitialMinersAliases.Split(',');
            foreach (var publicKey in publicKeys)
            {
                if (index >= aliases.Length)
                    return;

                var alias = aliases[index];
                SetAlias(publicKey, alias);
                index++;
            }
        }
        
        private void SetAlias(string publicKey, string alias)
        {
            State.AliasesMap[publicKey.ToStringValue()] = alias.ToStringValue();
            State.AliasesLookupMap[alias.ToStringValue()] = publicKey.ToStringValue();
        }

        private void UpdateBlockchainAge(long age)
        {
            //Assert(State.AgeField.Value <= age,
                //ContractErrorCode.GetErrorMessage(ContractErrorCode.AttemptFailed, "Cannot decrease blockchain age."));
            State.AgeField.Value = age;
        }

        public bool TryToGetCurrentAge(out long blockAge)
        {
            blockAge = State.AgeField.Value;
            return blockAge > 0;
        }

        private void SetBlockchainStartTimestamp(Timestamp timestamp)
        {
            Context.LogDebug(() => $"Set start timestamp to {timestamp}");
            State.BlockchainStartTimestamp.Value = timestamp;
        }

        public bool SetMiners(Miners miners, bool gonnaReplaceSomeone = false)
        {
            // Miners for one specific term should only update once.
            var m = State.MinersMap[miners.TermNumber.ToInt64Value()];
            if (gonnaReplaceSomeone || m == null)
            {
                State.MinersMap[miners.TermNumber.ToInt64Value()] = miners;
                return true;
            }

            return false;
        }

        private void LogVerbose(string log)
        {
            if (State.IsVerbose.Value)
            {
                Context.LogDebug(() => log);
            }
        }
    }
}
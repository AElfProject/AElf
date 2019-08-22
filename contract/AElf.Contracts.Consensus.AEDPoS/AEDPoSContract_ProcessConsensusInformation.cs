using System.Runtime.CompilerServices;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private void ProcessConsensusInformation(dynamic input, [CallerMemberName]string caller = null)
        {
            /* Privilege check. */
            if (!PreCheck())
            {
                return;
            }

            switch (input)
            {
                case Round round when caller == nameof(NextRound):
                    
                    break;
                case Round round when caller == nameof(NextTerm):
                    break;
                case UpdateValueInput updateValueInput:
                    break;
                case TinyBlockInput tinyBlockInput:
                    break;
            }

            ResetLatestProviderToTinyBlocksCount();
        }

        /// <summary>
        /// The transaction can still executed successfully if the pre-check failed,
        /// though doing nothing about updating state.
        /// </summary>
        /// <returns></returns>
        private bool PreCheck()
        {
            TryToGetCurrentRoundInformation(out var currentRound);
            
            _processingBlockMinerPubkey = Context.RecoverPublicKey().ToHex();
            if (!currentRound.IsInMinerList(_processingBlockMinerPubkey))
            {
                return false;
            }

            return true;
        }

        private void ResetLatestProviderToTinyBlocksCount()
        {
            var currentValue = State.LatestProviderToTinyBlocksCount.Value;
            if (currentValue.Pubkey == _processingBlockMinerPubkey)
            {
                if (currentValue.BlocksCount > 0)
                {
                    State.LatestProviderToTinyBlocksCount.Value = new LatestProviderToTinyBlocksCount
                    {
                        Pubkey = _processingBlockMinerPubkey,
                        BlocksCount = currentValue.BlocksCount.Sub(1)
                    };
                }
            }
            else
            {
                State.LatestProviderToTinyBlocksCount.Value = new LatestProviderToTinyBlocksCount
                {
                    Pubkey = _processingBlockMinerPubkey,
                    BlocksCount = GetTinyBlocksCount()
                };
            }
        }
    }
}
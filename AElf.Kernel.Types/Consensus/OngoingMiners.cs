using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable CheckNamespace
namespace AElf.Kernel
{
    public partial class OngoingMiners
    {
        /// <summary>
        /// Need to check the count of miners after.
        /// </summary>
        /// <param name="roundNumber"></param>
        /// <returns></returns>
        public Miners GetCurrentMiners(UInt64Value roundNumber)
        {
            return GetCurrentMiners(roundNumber.Value);
        }
        
        /// <summary>
        /// Need to check the count of miners after.
        /// </summary>
        /// <param name="roundNumber"></param>
        /// <returns></returns>
        public Miners GetCurrentMiners(ulong roundNumber)
        {
            return Miners.OrderByDescending(m => m.TakeEffectRoundNumber)
                .First(m => m.TakeEffectRoundNumber >= roundNumber);
        }

        public void UpdateMiners(UInt64Value takeEffectRoundNumber, IEnumerable<string> nextMinersAddresses)
        {
            if (Miners.Any(m => m.TakeEffectRoundNumber == takeEffectRoundNumber.Value))
            {
                return;
            }
            
            var nextMiners = new Miners
            {
                TakeEffectRoundNumber = takeEffectRoundNumber.Value
            };
            nextMiners.Nodes.AddRange(nextMinersAddresses);
            Miners.Add(nextMiners);
        }
        
        public void UpdateMiners(Miners nextMiners)
        {
            if (Miners.Contains(nextMiners))
            {
                return;
            }
            
            Miners.Add(nextMiners);
        }
    }
}
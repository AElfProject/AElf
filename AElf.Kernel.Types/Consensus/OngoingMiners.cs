using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;

// ReSharper disable CheckNamespace
namespace AElf.Kernel
{
    public partial class OngoingMiners
    {
        private readonly ILogger _logger = LogManager.GetLogger(nameof(OngoingMiners));
        
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
            //PrintMiners();
            
            return Miners.OrderByDescending(m => m.TakeEffectRoundNumber)
                .First(m => m.TakeEffectRoundNumber <= roundNumber);
        }

        public void UpdateMiners(UInt64Value takeEffectRoundNumber, IEnumerable<byte[]> pubKeys)
        {
            if (Miners.Any(m => m.TakeEffectRoundNumber >= takeEffectRoundNumber.Value))
            {
                return;
            }
            
            var nextMiners = new Miners
            {
                TakeEffectRoundNumber = takeEffectRoundNumber.Value
            };
            nextMiners.Producers.AddRange(pubKeys.Select(ByteString.CopyFrom));
            Miners.Add(nextMiners);
        }
        
        public void UpdateMiners(Miners nextMiners)
        {
            if (Miners.Any() && Miners.Contains(nextMiners))
            {
                return;
            }
            
            Miners.Add(nextMiners);
        }

        private void PrintMiners()
        {
            Console.WriteLine("Ongoing miners list:");
            foreach (var miner in Miners)
            {
                var str = "Take effect round number:" + miner.TakeEffectRoundNumber;
                foreach (var producer in miner.Producers)
                {
                    str += "\n" + producer.ToByteArray().ToPlainBase58();
                }

                Console.WriteLine(str);
            }

        }
    }
}
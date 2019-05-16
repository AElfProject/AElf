using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class AEDPoSInformationProvider : IAEDPoSInformationProvider
    {
        private readonly IReaderFactory _readerFactory;

        public AEDPoSInformationProvider(IReaderFactory readerFactory)
        {
            _readerFactory = readerFactory;
        }

        public async Task<IEnumerable<string>> GetCurrentMinerList(ChainContext chainContext)
        {
            var minersWithRoundNumber = await _readerFactory.Create(chainContext).GetCurrentMinerList.CallAsync(new Empty());
            return minersWithRoundNumber.PublicKeys.Select(k => k.ToHex());
        }
    }
}
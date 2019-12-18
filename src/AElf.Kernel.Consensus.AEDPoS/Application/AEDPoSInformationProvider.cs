using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    internal class AEDPoSInformationProvider : IAEDPoSInformationProvider
    {
        private readonly IConsensusReaderFactory _readerFactory;

        public AEDPoSInformationProvider(IConsensusReaderFactory readerFactory)
        {
            _readerFactory = readerFactory;
        }

        public async Task<IEnumerable<string>> GetCurrentMinerList(ChainContext chainContext)
        {
            var minersWithRoundNumber =
                await _readerFactory.Create(chainContext).GetCurrentMinerList.CallAsync(new Empty());
            return minersWithRoundNumber.Pubkeys.Select(k => k.ToHex());
        }
    }
}
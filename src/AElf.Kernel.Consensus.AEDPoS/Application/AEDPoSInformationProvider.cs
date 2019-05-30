using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class AEDPoSInformationProvider : IAEDPoSInformationProvider
    {
        private readonly IAEDPoSReaderFactory _readerFactory;

        public AEDPoSInformationProvider(IAEDPoSReaderFactory readerFactory)
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS.Application;

// ReSharper disable once InconsistentNaming
internal class AEDPoSInformationProvider : IAEDPoSInformationProvider
{
    private readonly IConsensusReaderContextService _consensusReaderContextService;
    private readonly IContractReaderFactory<AEDPoSContractContainer.AEDPoSContractStub> _contractReaderFactory;

    public AEDPoSInformationProvider(
        IContractReaderFactory<AEDPoSContractContainer.AEDPoSContractStub> contractReaderFactory,
        IConsensusReaderContextService consensusReaderContextService)
    {
        _contractReaderFactory = contractReaderFactory;
        _consensusReaderContextService = consensusReaderContextService;
    }

    public async Task<IEnumerable<string>> GetCurrentMinerListAsync(ChainContext chainContext)
    {
        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(chainContext);
        var minersWithRoundNumber =
            await _contractReaderFactory
                .Create(contractReaderContext).GetCurrentMinerList.CallAsync(new Empty());
        return minersWithRoundNumber.Pubkeys.Select(k => k.ToHex());
    }
}
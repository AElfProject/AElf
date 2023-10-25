using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS0;
using AElf.Standards.ACS6;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.WebAssembly;

public class WebAssemblyContractState : ContractState
{
    /// <summary>
    /// Hash Key -> Value
    /// </summary>
    public MappedState<Hash, BytesValue> Database { get; set; }

    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }

    internal RandomNumberProviderContractContainer.RandomNumberProviderContractReferenceState RandomNumberContract
    {
        get;
        set;
    }

    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal AEDPoSContractContainer.AEDPoSContractReferenceState ConsensusContract { get; set; }
}
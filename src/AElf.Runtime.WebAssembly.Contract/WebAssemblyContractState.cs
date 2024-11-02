using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.SolidityContract;
using AElf.Standards.ACS0;
using AElf.Standards.ACS10;
using AElf.Standards.ACS6;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.WebAssembly.Contract;

public class WebAssemblyContractState : ContractState
{
    /// <summary>
    /// Hash Key -> Value
    /// </summary>
    public MappedState<Hash, BytesValue> Database { get; set; }

    public BoolState Initialized { get; set; }
    public BoolState Terminated { get; set; }

    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    
    internal SolidityContractContainer.SolidityContractReferenceState SolidityContractManager { get; set; }

    internal RandomNumberProviderContractContainer.RandomNumberProviderContractReferenceState RandomNumberContract
    {
        get;
        set;
    }

    internal DividendPoolContractContainer.DividendPoolContractReferenceState TreasuryContract { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal AEDPoSContractContainer.AEDPoSContractReferenceState ConsensusContract { get; set; }
}
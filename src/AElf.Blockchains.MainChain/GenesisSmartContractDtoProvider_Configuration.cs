using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Vote;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForConfiguration(
            Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract(_codes.Single(kv => kv.Key.Contains("Configuration")).Value,
                ConfigurationSmartContractAddressNameProvider.Name);

            return l;
        }
    }
}
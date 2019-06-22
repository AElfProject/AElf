using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.TokenConverter;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForTokenConverter()
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract(
                _codes.Single(kv => kv.Key.Contains("TokenConverter")).Value,
                TokenConverterSmartContractAddressNameProvider.Name,
                GenerateTokenConverterInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateTokenConverterInitializationCallList()
        {
            var tokenConverterInitializationCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenConverterInitializationCallList.Add(
                nameof(TokenConverterContractContainer.TokenConverterContractStub.Initialize),
                new InitializeInput
                {
                    FeeRate = "0.01",
                    Connectors =
                    {
                        new Connector
                        {
                            Symbol = _tokenInitialOptions.Symbol,
                            IsPurchaseEnabled = true,
                            IsVirtualBalanceEnabled = true,
                            Weight = "0.5",
                            VirtualBalance = 0,
                        }
                    },
                    BaseTokenSymbol = _tokenInitialOptions.Symbol
                });
            return tokenConverterInitializationCallList;
        }
    }
}
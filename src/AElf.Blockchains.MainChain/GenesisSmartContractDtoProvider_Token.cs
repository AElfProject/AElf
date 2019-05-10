using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForToken(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();
//            l.AddGenesisSmartContract<TokenContract>(
            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("MultiToken")).Value,
                TokenSmartContractAddressNameProvider.Name,
                GenerateTokenInitializationCallList(zeroContractAddress));
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList(
            Address issuer)
        {
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = _tokenInitialOptions.Symbol,
                Decimals = _tokenInitialOptions.Decimals,
                IsBurnable = _tokenInitialOptions.IsBurnable,
                TokenName = _tokenInitialOptions.Name,
                TotalSupply = _tokenInitialOptions.TotalSupply,
                // Set the contract zero address as the issuer temporarily.
                Issuer = issuer,
                LockWhiteSystemContractNameList = {ConsensusSmartContractAddressNameProvider.Name}
            });

            tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = _tokenInitialOptions.Symbol,
                Amount = (long) (_tokenInitialOptions.TotalSupply * _tokenInitialOptions.DividendPoolRatio),
                ToSystemContractName = DividendSmartContractAddressNameProvider.Name,
                Memo = "Set dividends.",
            });

            //TODO: Maybe should be removed after testing.
            foreach (var tokenReceiver in _dposOptions.InitialMiners)
            {
                tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
                {
                    Symbol = _tokenInitialOptions.Symbol,
                    Amount = (long) (_tokenInitialOptions.TotalSupply * (1 - _tokenInitialOptions.DividendPoolRatio)) /
                             _dposOptions.InitialMiners.Count,
                    To = Address.FromPublicKey(ByteArrayHelpers.FromHexString(tokenReceiver)),
                    Memo = "Set initial miner's balance.",
                });
            }

            // Set fee pool address to dividend contract address.
            tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.SetFeePoolAddress),
                DividendSmartContractAddressNameProvider.Name);

            tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.InitializeTokenContract), new IntializeTokenContractInput
            {
                CrossChainContractSystemName = CrossChainSmartContractAddressNameProvider.Name
            });
            return tokenContractCallList;
        }

    }
}
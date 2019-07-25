using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using AElf.Kernel.Consensus.AEDPoS;

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
                LockWhiteSystemContractNameList =
                {
                    ElectionSmartContractAddressNameProvider.Name,
                    VoteSmartContractAddressNameProvider.Name,
                    ProfitSmartContractAddressNameProvider.Name,
                }
            });

            tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = _tokenInitialOptions.Symbol,
                Amount = (long) (_tokenInitialOptions.TotalSupply * _tokenInitialOptions.DividendPoolRatio),
                ToSystemContractName = ElectionSmartContractAddressNameProvider.Name,
                Memo = "Set dividends.",
            });

            //TODO: Maybe should be removed after testing.
            foreach (var tokenReceiver in _consensusOptions.InitialMiners)
            {
                tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
                {
                    Symbol = _tokenInitialOptions.Symbol,
                    Amount = (long) (_tokenInitialOptions.TotalSupply * (1 - _tokenInitialOptions.DividendPoolRatio)) /
                             _consensusOptions.InitialMiners.Count,
                    To = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(tokenReceiver)),
                    Memo = "Set initial miner's balance."
                });
            }

            // Set fee pool address to election contract address.
            tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.SetFeePoolAddress),
                ElectionSmartContractAddressNameProvider.Name);
            return tokenContractCallList;
        }

    }
}
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using InitializeInput = AElf.Contracts.TokenConverter.InitializeInput;

namespace AElf.Contracts.Economic
{
    public class EconomicContract : EconomicContractContainer.EconomicContractBase
    {
        public override Empty InitialEconomicSystem(InitialEconomicSystemInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

            Context.LogDebug(() => "Will create tokens.");
            CreateNativeToken(input);
            CreateTokenConverterToken();
            CreateResourceTokens();
            CreateElectionToken();

            Context.LogDebug(() => "Finished creating tokens.");

            InitialMiningReward(input.MiningRewardTotalAmount);

            RegisterElectionVotingEvent();
            SetTreasurySchemeIdsToElectionContract();
            SetResourceTokenUnitPrice();

            InitializeTokenConverterContract();

            State.Initialized.Value = true;
            return new Empty();
        }

        private void CreateNativeToken(InitialEconomicSystemInput input)
        {
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = input.NativeTokenSymbol,
                TokenName = "Native Token",
                TotalSupply = input.NativeTokenTotalSupply,
                Decimals = input.NativeTokenDecimals,
                IsBurnable = input.IsNativeTokenBurnable,
                Issuer = Context.Self,
                LockWhiteList =
                {
                    Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName),
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName),
                    Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName),
                    Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName),
                    Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName)
                }
            });
        }

        private void CreateTokenConverterToken()
        {
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = EconomicContractConstants.TokenConverterTokenSymbol,
                TokenName = "AElf Token Converter Token",
                TotalSupply = EconomicContractConstants.TokenConverterTokenTotalSupply,
                Decimals = EconomicContractConstants.TokenConverterTokenDecimals,
                Issuer = Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName),
                IsBurnable = true,
                LockWhiteList =
                {
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName),
                    Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName)
                }
            });
        }

        private void CreateResourceTokens()
        {
            var tokenConverter =
                Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);
            foreach (var resourceTokenSymbol in Context.Variables.ResourceTokenSymbolNameList)
            {
                State.TokenContract.Create.Send(new CreateInput
                {
                    Symbol = resourceTokenSymbol,
                    TokenName = $"{resourceTokenSymbol} Token",
                    TotalSupply = EconomicContractConstants.ResourceTokenTotalSupply,
                    Decimals = EconomicContractConstants.ResourceTokenDecimals,
                    Issuer = Context.Self,
                    LockWhiteList =
                    {
                        Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName),
                        Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName)
                    },
                    IsBurnable = true // TODO: TBD,
                });
                
                State.TokenContract.Issue.Send(new IssueInput
                {
                    Symbol = resourceTokenSymbol,
                    Amount = EconomicContractConstants.ResourceTokenTotalSupply,
                    To = tokenConverter,
                    Memo = "Initialize for resource trade"
                });
            }
        }

        private void CreateElectionToken()
        {
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = EconomicContractConstants.ElectionTokenSymbol,
                TokenName = "Election Token",
                TotalSupply = EconomicContractConstants.ElectionTokenTotalSupply,
                Decimals = 0,
                Issuer = Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName),
                IsBurnable = false,
                LockWhiteList =
                {
                    Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName),
                    Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName)
                }
            });
        }

        /// <summary>
        /// Only contract owner of Economic Contract can issue native token.
        /// Mainly for testing.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty IssueNativeToken(IssueNativeTokenInput input)
        {
            if (State.ZeroContract.Value == null)
            {
                State.ZeroContract.Value = Context.GetZeroSmartContractAddress();
            }

            var contractOwner = State.ZeroContract.GetContractAuthor.Call(Context.Self);
            if (contractOwner != Context.Sender)
            {
                return new Empty();
            }

            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = Context.Variables.NativeSymbol,
                Amount = input.Amount,
                To = input.To,
                Memo = input.Memo
            });
            return new Empty();
        }

        /// <summary>
        /// Transfer all the tokens prepared for rewarding mining to consensus contract.
        /// </summary>
        /// <param name="miningRewardAmount"></param>
        private void InitialMiningReward(long miningRewardAmount)
        {
            var consensusContractAddress =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

            State.TokenContract.Issue.Send(new IssueInput
            {
                To = consensusContractAddress,
                Amount = miningRewardAmount,
                Symbol = Context.Variables.NativeSymbol,
                Memo = "Initial mining reward."
            });
        }

        private void RegisterElectionVotingEvent()
        {
            State.ElectionContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
            State.ElectionContract.RegisterElectionVotingEvent.Send(new Empty());
        }

        private void SetTreasurySchemeIdsToElectionContract()
        {
            State.ProfitContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
            var schemeIdsManagingByTreasuryContract = State.ProfitContract.GetManagingSchemeIds.Call(
                new GetManagingSchemeIdsInput
                {
                    Manager = Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName)
                }).SchemeIds;
            var schemeIdsManagingByElectionContract = State.ProfitContract.GetManagingSchemeIds.Call(
                new GetManagingSchemeIdsInput
                {
                    Manager = Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName)
                }).SchemeIds;
            State.ElectionContract.SetTreasurySchemeIds.Send(new SetTreasurySchemeIdsInput
            {
                TreasuryHash = schemeIdsManagingByTreasuryContract[0],
                VotesRewardHash = schemeIdsManagingByTreasuryContract[3],
                ReElectionRewardHash = schemeIdsManagingByTreasuryContract[4],
                SubsidyHash = schemeIdsManagingByElectionContract[0],
                WelfareHash = schemeIdsManagingByElectionContract[1]
            });
        }

        private void SetResourceTokenUnitPrice()
        {
            State.TokenContract.SetResourceTokenUnitPrice.Send(new SetResourceTokenUnitPriceInput
            {
                CpuUnitPrice = EconomicContractConstants.CpuUnitPrice,
                StoUnitPrice = EconomicContractConstants.StoUnitPrice,
                NetUnitPrice = EconomicContractConstants.NetUnitPrice,
            });
        }

        private Address InitialConnectorManager()
        {
            State.ParliamentAuthContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);

            var createOrganizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = EconomicContractConstants.ConnectorSettingProposalReleaseThreshold
            };
            State.ParliamentAuthContract.CreateOrganization.Send(createOrganizationInput);

            var organizationHash = Hash.FromTwoHashes(Hash.FromMessage(State.ParliamentAuthContract.Value),
                Hash.FromMessage(createOrganizationInput));
            return Address.FromPublicKey(State.ParliamentAuthContract.Value.Value.Concat(
                organizationHash.Value.ToByteArray().ComputeHash()).ToArray());
        }

        private void InitializeTokenConverterContract()
        {
            var connectorManager = InitialConnectorManager();
            State.TokenConverterContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);
            var connectors = new List<Connector>
            {
                new Connector
                {
                    Symbol = Context.Variables.NativeSymbol,
                    IsPurchaseEnabled = true,
                    IsVirtualBalanceEnabled = true,
                    Weight = "0.5",
                    VirtualBalance = EconomicContractConstants.NativeTokenConnectorInitialVirtualBalance
                },
                new Connector
                {
                    Symbol = EconomicContractConstants.TokenConverterTokenSymbol,
                    IsPurchaseEnabled = true,
                    IsVirtualBalanceEnabled = true,
                    Weight = "0.5",
                    VirtualBalance = EconomicContractConstants.TokenConverterTokenConnectorInitialVirtualBalance
                }
            };

            foreach (var resourceTokenSymbol in Context.Variables.ResourceTokenSymbolNameList)
            {
                connectors.Add(new Connector
                {
                    Symbol = resourceTokenSymbol,
                    IsPurchaseEnabled = true,
                    IsVirtualBalanceEnabled = true,
                    Weight = EconomicContractConstants.ResourceTokenConnectorWeight,
                    VirtualBalance = EconomicContractConstants.ResourceTokenConnectorInitialVirtualBalance
                });
            }

            State.TokenConverterContract.Initialize.Send(new InitializeInput
            {
                FeeRate = EconomicContractConstants.TokenConverterFeeRate,
                Connectors = {connectors},
                BaseTokenSymbol = Context.Variables.NativeSymbol,
                ManagerAddress = connectorManager
            });
        }
    }
}
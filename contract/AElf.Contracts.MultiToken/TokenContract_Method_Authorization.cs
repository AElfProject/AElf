using System.Collections.Generic;
using Acs3;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        // private const string ExtraAvailableToken = "ExtraAvailableToken";
        // private const string CoefficientUpdate = "CoefficientUpdate";

        #region orgnanization init

        private void InitializeOrganization(Address defaultProposer)
        {
            if (State.NormalOrganizationForToken.Value != null &&
                State.ReferendumOrganizationForCoefficient.Value != null &&
                State.ParliamentOrganizationForCoefficient.Value != null)
                return;
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            if (State.AssociationContract.Value == null)
            {
                State.AssociationContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.AssociationContractSystemName);
            }

            if (State.ReferendumContract.Value == null)
            {
                State.ReferendumContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ReferendumContractSystemName);
            }

            State.DefaultProposer.Value = defaultProposer == null
                ? State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty())
                : defaultProposer;
            
            State.ParliamentOrganizationForCoefficient.Value =
                State.ParliamentContract.CalculateOrganizationAddress.Call(GetParliamentOrganizationForCoefficientInput()
                    .OrganizationCreationInput);
            State.ReferendumOrganizationForCoefficient.Value =
                State.ReferendumContract.CalculateOrganizationAddress.Call(GetOrganizationForCoefficientInput()
                    .OrganizationCreationInput);

            State.ParliamentOrganizationForExtraToken.Value =
                State.ParliamentContract.CalculateOrganizationAddress.Call(GetParliamentOrganizationForExtraTokenInput()
                    .OrganizationCreationInput);
            State.NormalOrganizationForToken.Value =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetNormalOrganizationForTokenInput()
                    .OrganizationCreationInput);
            
            CreateParliamentOrganizationForCoefficient();
            CreateOrganizationForUpdateCoefficient();
            CreateAssociationOrganizationForUpdateCoefficient();
            
            if(State.ParliamentOrganizationForExtraToken.Value != State.ParliamentOrganizationForCoefficient.Value)
                CreateParliamentOrganizationForExtraToken();
            CreateOrganizationForUpdateAvailableToken();
            CreateAssociationOrganizationForUpdateExtraToken();
        }
        
        private void CreateParliamentOrganizationForCoefficient()
        {
            State.ParliamentContract.CreateOrganizationBySystemContract.Send(
                GetParliamentOrganizationForCoefficientInput());
        }

        private void CreateOrganizationForUpdateCoefficient()
        {
            State.ReferendumContract.CreateOrganizationBySystemContract.Send(GetOrganizationForCoefficientInput());
        }

        private void CreateAssociationOrganizationForUpdateCoefficient()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetAssociationOrganizationForCoefficientInput());
        }

        private void CreateParliamentOrganizationForExtraToken()
        {
            State.ParliamentContract.CreateOrganizationBySystemContract.Send(
                GetParliamentOrganizationForExtraTokenInput());
        }

        private void CreateOrganizationForUpdateAvailableToken()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetNormalOrganizationForTokenInput());
        }

        private void CreateAssociationOrganizationForUpdateExtraToken()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetAssociationOrganizationForExtraTokenInput());
        }

        #endregion

        #region organization create input

        private Parliament.CreateOrganizationBySystemContractInput GetParliamentOrganizationForCoefficientInput()
        {
            return new Parliament.CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Parliament.CreateOrganizationInput
                {
                    ProposerAuthorityRequired = true,
                    ParliamentMemberProposingAllowed = true,
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 7,
                        MaximalRejectionThreshold = 8,
                        MinimalApprovalThreshold = 2,
                        MinimalVoteThreshold = 2
                    }
                }
            };
        }

        private Referendum.CreateOrganizationBySystemContractInput GetOrganizationForCoefficientInput()
        {
            var whiteList = new List<Address> {State.DefaultProposer.Value, State.AssociationContract.Value};
            return new Referendum.CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Referendum.CreateOrganizationInput
                {
                    TokenSymbol = State.NativeTokenSymbol.Value,
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = whiteList.Count, // todo
                        MinimalVoteThreshold = whiteList.Count, //todo
                        MaximalRejectionThreshold = 0,
                        MaximalAbstentionThreshold = 0
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {whiteList}
                    }
                }
            };
        }

        private Association.CreateOrganizationBySystemContractInput GetAssociationOrganizationForCoefficientInput()
        {
            var proposers = new List<Address>
                {State.ReferendumOrganizationForCoefficient.Value, State.ParliamentOrganizationForCoefficient.Value};
            var proposerAddress = State.DefaultProposer.Value;
            return new Association.CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Association.CreateOrganizationInput
                {
                    OrganizationMemberList = new Association.OrganizationMemberList
                    {
                        OrganizationMembers = {proposers}
                    },
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = proposers.Count,
                        MinimalVoteThreshold = proposers.Count,
                        MaximalRejectionThreshold = 0,
                        MaximalAbstentionThreshold = 0
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {proposerAddress, State.AssociationContract.Value}
                    }
                },
                OrganizationAddressFeedbackMethod = nameof(SetAssociateOrganizationForCoefficient)

            };
        }

        private Parliament.CreateOrganizationBySystemContractInput GetParliamentOrganizationForExtraTokenInput()
        {
            return new Parliament.CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Parliament.CreateOrganizationInput
                {
                    ProposerAuthorityRequired = true,
                    ParliamentMemberProposingAllowed = true,
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 7,
                        MaximalRejectionThreshold = 8,
                        MinimalApprovalThreshold = 2,
                        MinimalVoteThreshold = 2
                    }
                }
            };
        }

        private Association.CreateOrganizationBySystemContractInput GetNormalOrganizationForTokenInput()
        {
            var proposerAddress = State.DefaultProposer.Value;
            var proposers = new List<Address> {proposerAddress};
            var whiteList = new List<Address> {proposerAddress, State.AssociationContract.Value};
            return new Association.CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Association.CreateOrganizationInput
                {
                    OrganizationMemberList = new Association.OrganizationMemberList
                    {
                        OrganizationMembers = {proposers}
                    },
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = proposers.Count,
                        MinimalVoteThreshold = proposers.Count,
                        MaximalRejectionThreshold = 0,
                        MaximalAbstentionThreshold = 0
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {whiteList}
                    }
                }
            };
        }

        private Association.CreateOrganizationBySystemContractInput GetAssociationOrganizationForExtraTokenInput()
        {
            var proposers = new List<Address>
            {
                State.DefaultProposer.Value, State.NormalOrganizationForToken.Value, State.ParliamentOrganizationForExtraToken.Value
            };
            var proposerWhiteList = new List<Address>
            {
                State.DefaultProposer.Value
            };
            return new Association.CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Association.CreateOrganizationInput
                {
                    OrganizationMemberList = new Association.OrganizationMemberList
                    {
                        OrganizationMembers = {proposers}
                    },
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = proposers.Count,
                        MinimalVoteThreshold = proposers.Count,
                        MaximalRejectionThreshold = 0,
                        MaximalAbstentionThreshold = 0
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {proposerWhiteList}
                    }
                },
                OrganizationAddressFeedbackMethod = nameof(SetAssociateOrganizationForExtraToken)
            };
        }

        #endregion
        
        #region recall back for setting organization and proposal id
        
        public override Empty SetAssociateOrganizationForCoefficient(Address input)
        {
            Assert(input != null, "invalid address");
            State.AssociationOrganizationForCoefficient.Value = input;
            return new Empty();
        }
        public override Empty SetAssociateOrganizationForExtraToken(Address input)
        {
            Assert(input != null, "invalid address");
            State.AssociationOrganizationForToken.Value = input;
            return new Empty();
        }
        #endregion

        #region proposal about available token list setting

        public override Empty SubmitAddAvailableTokenInfoProposal(AvailableTokenInfo tokenInfo)
        {
            SendSubmitForUpdateExtraToken(tokenInfo, nameof(AddAvailableTokenInfo));
            return new Empty();
        }

        public override Empty SubmitRemoveAvailableTokenInfoProposal(StringValue tokenInfo)
        {
            SendSubmitForUpdateExtraToken(null, nameof(RemoveAvailableTokenInfo), tokenInfo);
            return new Empty();
        }

        public override Empty SubmitUpdateAvailableTokenInfoProposal(AvailableTokenInfo tokenInfo)
        {
            SendSubmitForUpdateExtraToken(tokenInfo, nameof(UpdateAvailableTokenInfo));
            return new Empty();
        }

        public override Empty SetExtraAvailableTokenProposal(Hash proposalId)
        {
            Assert(Context.Sender == State.AssociationContract.Value, "not be authorized to call this method");
            // State.ProposalMap[ExtraAvailableToken] = proposalId;
            State.AssociationContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                OriginProposer = State.DefaultProposer.Value,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = State.AssociationContract.Value,
                    OrganizationAddress = State.NormalOrganizationForToken.Value,
                    Params = proposalId.ToByteString(),
                    ContractMethodName = nameof(Approve),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1)
                }
            });
            State.ParliamentContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                OriginProposer = State.DefaultProposer.Value,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = State.AssociationContract.Value,
                    OrganizationAddress = State.ParliamentOrganizationForExtraToken.Value,
                    Params = proposalId.ToByteString(),
                    ContractMethodName = nameof(Approve),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1)
                }
            });
            return new Empty();
        }

        private void SendSubmitForUpdateExtraToken(AvailableTokenInfo tokenInfo, string recallMethod,
            StringValue tokenSymbol = null)
        {
            State.AssociationContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                OriginProposer = Context.Sender,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = Context.Self,
                    OrganizationAddress = State.AssociationOrganizationForToken.Value,
                    Params = tokenSymbol == null ? tokenInfo.ToByteString() : tokenSymbol.ToByteString(),
                    ContractMethodName = recallMethod,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1)
                },
                ProposalIdFeedbackMethod = nameof(SetExtraAvailableTokenProposal)
            });
        }

        #endregion

        #region proposal about coefficient setting

        public override Empty SubmitUpdateCoefficientFromContractProposal(CoefficientFromContract coefficientInput)
        {
            SendSubmitForUpdateCoefficient(coefficientInput, null, nameof(UpdateCoefficientFromContract));
            return new Empty();
        }

        public override Empty SubmitUpdateCoefficientFromSenderProposal(CoefficientFromSender coefficientInput)
        {
            SendSubmitForUpdateCoefficient(null, coefficientInput, nameof(UpdateCoefficientFromSender));
            return new Empty();
        }

        public override Empty SetCoefficientTokenProposal(Hash proposalId)
        {
            Assert(Context.Sender == State.AssociationContract.Value, "not be authorized to call this method");
            // State.ProposalMap[ExtraAvailableToken] = proposalId;
            State.ReferendumContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                OriginProposer = State.DefaultProposer.Value,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = State.AssociationContract.Value,
                    OrganizationAddress = State.ReferendumOrganizationForCoefficient.Value,
                    Params = proposalId.ToByteString(),
                    ContractMethodName = nameof(Approve),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1)
                }
            });
            State.ParliamentContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                OriginProposer = State.DefaultProposer.Value,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = State.AssociationContract.Value,
                    OrganizationAddress = State.ParliamentOrganizationForCoefficient.Value,
                    Params = proposalId.ToByteString(),
                    ContractMethodName = nameof(Approve),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1)
                }
            });
            return new Empty();
        }

        private void SendSubmitForUpdateCoefficient(CoefficientFromContract coefficientFromContractInput,
            CoefficientFromSender coefficientFromSenderInput, string recallMethod)
        {
            State.AssociationContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                OriginProposer = Context.Sender,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = Context.Self,
                    OrganizationAddress = State.AssociationOrganizationForCoefficient.Value,
                    Params = coefficientFromContractInput == null
                        ? coefficientFromSenderInput.ToByteString()
                        : coefficientFromContractInput.ToByteString(),
                    ContractMethodName = recallMethod,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1)
                },
                ProposalIdFeedbackMethod = nameof(SetCoefficientTokenProposal)
            });
        }

        #endregion
    }
}
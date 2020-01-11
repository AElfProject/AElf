using System.Collections.Generic;
using Acs3;
using AElf.Contracts.Association;
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

        private void InitializeOrganization()
        {
            if (State.OrganizationForUpdateExtraAvailableToken.Value != null &&
                State.OrganizationForUpdateCoefficient.Value != null && State.ParliamentOrganization.Value != null)
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

            CalculateAndInitializeOrganizationAddress();
            
            if (State.ParliamentOrganization.Value == null)
                CreateParliamentOrganization();
            if (State.OrganizationForUpdateCoefficient.Value == null)
                CreateOrganizationForUpdateCoefficient();
            if (State.OrganizationForUpdateExtraAvailableToken.Value == null)
                CreateOrganizationForUpdateAvailableToken();
            if (State.AssociationOrganizationForExtraAvailableToken.Value == null)
                CreateAssociationOrganizationForUpdateExtraToken();
            if (State.AssociationOrganizationForUpdateCoefficient.Value == null)
                CreateAssociationOrganizationForUpdateCoefficient();
        }

        private void CalculateAndInitializeOrganizationAddress()
        {
            State.ParliamentOrganization.Value = State.AssociationContract.CalculateOrganizationAddress.Call(GetParliamentOrganizationInput());
            State.OrganizationForUpdateCoefficient.Value = State.ReferendumContract.CalculateOrganizationAddress.Call(GetOrganizationForUpdateUpdateCoefficientInput());
            State.OrganizationForUpdateExtraAvailableToken.Value = State.AssociationContract.CalculateOrganizationAddress.Call(GetOrganizationForUpdateAvailableTokenInput());
            State.AssociationOrganizationForExtraAvailableToken.Value = State.AssociationContract.CalculateOrganizationAddress.Call(GetAssociationOrganizationForExtraAvailableTokenInput());;
            State.AssociationOrganizationForUpdateCoefficient.Value = State.AssociationContract.CalculateOrganizationAddress.Call(GetAssociationOrganizationForUpdateCoefficientInput());
        }
        // private void CreateParliamentOrganization()
        // {
        //     State.ParliamentOrganization.Value = State.ParliamentContract.CreateOrganization.Call(
        //         new Parliament.CreateOrganizationInput
        //         {
        //             ProposerAuthorityRequired = true,
        //             ProposalReleaseThreshold = new ProposalReleaseThreshold
        //             {
        //                 MinimalApprovalThreshold = 5, //todo
        //                 MinimalVoteThreshold = 8,
        //                 MaximalRejectionThreshold = 0,
        //                 MaximalAbstentionThreshold = 0
        //             },
        //             ParliamentMemberProposingAllowed = true
        //         });
        // }
        private void CreateParliamentOrganization()
        {
            State.AssociationContract.CreateOrganization.Send(GetParliamentOrganizationInput());
        }

        private CreateOrganizationInput GetParliamentOrganizationInput()
        {
            var proposerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            var proposers = new List<Address> {proposerAddress};
            var whiteList = new List<Address> {proposerAddress, Context.Self};
            return new CreateOrganizationInput
            {
                OrganizationMemberList = new OrganizationMemberList
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
            };
        }

        private void CreateOrganizationForUpdateCoefficient()
        {
            State.ReferendumContract.CreateOrganization.Send(GetOrganizationForUpdateUpdateCoefficientInput());
        }

        private Referendum.CreateOrganizationInput GetOrganizationForUpdateUpdateCoefficientInput()
        {
            var proposerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            var whiteList = new List<Address> {proposerAddress, Context.Self};
            return new Referendum.CreateOrganizationInput
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
            };
        }
        private void CreateOrganizationForUpdateAvailableToken()
        {
            State.AssociationContract.CreateOrganization.Send(GetOrganizationForUpdateAvailableTokenInput());
        }
        
        private CreateOrganizationInput GetOrganizationForUpdateAvailableTokenInput()
        {
            var proposerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            var proposers = new List<Address> {proposerAddress};
            var whiteList = new List<Address> {proposerAddress, Context.Self};
            return new CreateOrganizationInput
            {
                OrganizationMemberList = new OrganizationMemberList
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
            };
        }
        
        private void CreateAssociationOrganizationForUpdateCoefficient()
        {
            State.AssociationContract.CreateOrganization.Send(GetAssociationOrganizationForUpdateCoefficientInput());
        }
        private CreateOrganizationInput GetAssociationOrganizationForUpdateCoefficientInput()
        {
            var proposers = new List<Address> { State.OrganizationForUpdateCoefficient.Value, State.ParliamentOrganization.Value };
            return new CreateOrganizationInput
            {
                OrganizationMemberList = new OrganizationMemberList
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
                    Proposers = {Context.Self}
                }
            };
        }
        private void CreateAssociationOrganizationForUpdateExtraToken()
        {
            State.AssociationContract.CreateOrganization.Send(GetAssociationOrganizationForExtraAvailableTokenInput());
        }
        private CreateOrganizationInput GetAssociationOrganizationForExtraAvailableTokenInput()
        {
            var proposers = new List<Address> { State.OrganizationForUpdateExtraAvailableToken.Value, State.ParliamentOrganization.Value };
            return new CreateOrganizationInput
            {
                OrganizationMemberList = new OrganizationMemberList
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
                    Proposers = {Context.Self}
                }
            };
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
            Assert(Context.Sender == Context.Self, "not be authorized to call this method");
            // State.ProposalMap[ExtraAvailableToken] = proposalId;
            State.AssociationContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                OriginProposer = Context.Sender,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = State.AssociationContract.Value,
                    OrganizationAddress = State.OrganizationForUpdateExtraAvailableToken.Value,
                    Params = proposalId.ToByteString(),
                    ContractMethodName = nameof(Approve),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1)
                }
            });
            State.ParliamentContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                OriginProposer = Context.Sender,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = State.AssociationContract.Value,
                    OrganizationAddress = State.ParliamentOrganization.Value,
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
                    OrganizationAddress = State.AssociationOrganizationForExtraAvailableToken.Value,
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
            Assert(Context.Sender == Context.Self, "not be authorized to call this method");
            // State.ProposalMap[ExtraAvailableToken] = proposalId;
            State.ReferendumContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                OriginProposer = Context.Sender,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = State.AssociationContract.Value,
                    OrganizationAddress = State.OrganizationForUpdateCoefficient.Value,
                    Params = proposalId.ToByteString(),
                    ContractMethodName = nameof(Approve),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1)
                }
            });
            State.ParliamentContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                OriginProposer = Context.Sender,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = State.AssociationContract.Value,
                    OrganizationAddress = State.ParliamentOrganization.Value,
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
            State.AssociationContract.CreateProposalBySystemContract.Call(new CreateProposalBySystemContractInput
            {
                OriginProposer = Context.Sender,
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = Context.Self,
                    OrganizationAddress = State.AssociationOrganizationForUpdateCoefficient.Value,
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
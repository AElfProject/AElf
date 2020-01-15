using System.Collections.Generic;
using Acs3;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        #region orgnanization init

        public override Empty InitializeAuthorizedOrganization(Empty input)
        {
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
            var defaultParliamentAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            if (State.DefaultProposer.Value == null)
                State.DefaultProposer.Value = defaultParliamentAddress;
            State.UserFeeAssociationOrganization.Value = new UserFeeProposerOrganization();
            State.UserFeeAssociationOrganization.Value.ParliamentOrganization = defaultParliamentAddress;
            State.UserFeeAssociationOrganization.Value.ReferendumOrganization =
                State.ReferendumContract.CalculateOrganizationAddress.Call(GetReferendumOrganizationForUserFee()
                    .OrganizationCreationInput);
            State.UserFeeAssociationOrganization.Value.RootOrganization =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetAssociationOrganizationForUserFee()
                    .OrganizationCreationInput);

            State.DeveloperFeeAssociationOrganization.Value = new DeveloperFeeProposerOrganization();
            State.DeveloperFeeAssociationOrganization.Value.ParliamentOrganization = defaultParliamentAddress;
            State.DeveloperFeeAssociationOrganization.Value.DeveloperOrganization =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetDeveloperOrganization()
                    .OrganizationCreationInput);
            State.DeveloperFeeAssociationOrganization.Value.RootOrganization =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetAssociationOrganizationForDeveloperFee()
                    .OrganizationCreationInput);
            
            CreateReferendumOrganizationForUserFee();
            CreateAssociationOrganizationForUserFee();
            
            CreateDeveloperOrganization();
            CreateAssociationOrganizationForDeveloperFee();
            return new Empty();
        }
        private void CreateReferendumOrganizationForUserFee()
        {
            State.ReferendumContract.CreateOrganizationBySystemContract.Send(GetReferendumOrganizationForUserFee());
        }

        private void CreateAssociationOrganizationForUserFee()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetAssociationOrganizationForUserFee());
        }
        
        private void CreateDeveloperOrganization()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetDeveloperOrganization());
        }

        private void CreateAssociationOrganizationForDeveloperFee()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetAssociationOrganizationForDeveloperFee());
        }

        #endregion

        #region organization create input
        private Referendum.CreateOrganizationBySystemContractInput GetReferendumOrganizationForUserFee()
        {
            var parliamentOrg = State.UserFeeAssociationOrganization.Value.ParliamentOrganization;
            var whiteList = new List<Address> {parliamentOrg};
            if(State.DefaultProposer.Value != null && State.DefaultProposer.Value != parliamentOrg)
                whiteList.Add(State.DefaultProposer.Value);
            return new Referendum.CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Referendum.CreateOrganizationInput
                {
                    TokenSymbol = "EE",
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 1,
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

        private Association.CreateOrganizationBySystemContractInput GetAssociationOrganizationForUserFee()
        {
            var parliamentOrg = State.UserFeeAssociationOrganization.Value.ParliamentOrganization;
            var proposers = new List<Address>
                {State.UserFeeAssociationOrganization.Value.ReferendumOrganization, parliamentOrg};
            var actualProposalCount = proposers.Count;
            if (State.DefaultProposer.Value != null && State.DefaultProposer.Value != parliamentOrg)
            {
                proposers.Add(State.DefaultProposer.Value);
            }
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
                        MinimalApprovalThreshold = actualProposalCount,
                        MinimalVoteThreshold = actualProposalCount,
                        MaximalRejectionThreshold = 0,
                        MaximalAbstentionThreshold = 0
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {proposers}
                    }
                }
            };
        }
        private Association.CreateOrganizationBySystemContractInput GetDeveloperOrganization()
        {
            var parliamentOrganization = State.DeveloperFeeAssociationOrganization.Value.ParliamentOrganization;
            var proposers = new List<Address> {parliamentOrganization};
            if(State.DefaultProposer.Value != null && State.DefaultProposer.Value != parliamentOrganization)
                proposers.Add(State.DefaultProposer.Value);
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
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 1,
                        MaximalRejectionThreshold = 0,
                        MaximalAbstentionThreshold = 0
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {proposers}
                    }
                }
            };
        }

        private Association.CreateOrganizationBySystemContractInput GetAssociationOrganizationForDeveloperFee()
        {
            var parliamentOrg = State.DeveloperFeeAssociationOrganization.Value.ParliamentOrganization;
            var proposers = new List<Address>
            {
                State.DeveloperFeeAssociationOrganization.Value.DeveloperOrganization, parliamentOrg
            };
            var actualProposalCount = proposers.Count;
            if (State.DefaultProposer.Value != null && parliamentOrg != State.DefaultProposer.Value)
            {
                 proposers.Add(State.DefaultProposer.Value);
            }
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
                        MinimalApprovalThreshold = actualProposalCount,
                        MinimalVoteThreshold = actualProposalCount,
                        MaximalRejectionThreshold = 0,
                        MaximalAbstentionThreshold = 0
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {proposers}
                    }
                }
            };
        }

        #endregion
    }
}
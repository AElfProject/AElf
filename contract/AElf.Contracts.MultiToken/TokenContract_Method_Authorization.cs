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

        public override Empty InitializeAuthorizedController(Empty input)
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
            if(ControllersInitialized())
                return new Empty();
            CalculateUserFeeController();
            CreateReferendumControllerForUserFee();
            CreateAssociationControllerForUserFee();
            
            CalculateDeveloperFeeController();
            CreateDeveloperController();
            CreateAssociationControllerForDeveloperFee();
            return new Empty();
        }
        
        public override Empty SetControllerForSymbolsToPayTXSizeFee(Address input)
        {
            AssertControllerForSymbolToPayTxSizeFee();
            Assert(input != null, "invalid input");
            var isNewControllerIsExist = State.ParliamentContract.ValidateOrganizationExist.Call(input);
            Assert(isNewControllerIsExist.Value, "new controller does not exist");
            State.ControllerForSymbolToPayTxFee.Value = input;
            return new Empty();
        }
        
        public override Empty SetControllerForSideChainParliament(Address input)
        {
            AssertControllerForSideChainRental();
            Assert(input != null, "invalid input");
            var isNewControllerIsExist = State.ParliamentContract.ValidateOrganizationExist.Call(input);
            Assert(isNewControllerIsExist.Value, "new controller does not exist");
            State.ControllerForSideRentalParliament.Value = input;
            return new Empty();
        }
        
        private bool ControllersInitialized()
        {
            if(State.ControllerForDeveloperFee.Value == null)
                State.ControllerForDeveloperFee.Value = new ControllerForDeveloperFee();
            if(State.ControllerForUserFee.Value == null)
                State.ControllerForUserFee.Value = new ControllerForUserFee();
            return !(State.ControllerForDeveloperFee.Value.DeveloperController == null ||
                    State.ControllerForDeveloperFee.Value.ParliamentController == null ||
                    State.ControllerForDeveloperFee.Value.RootController == null ||
                    State.ControllerForUserFee.Value.ParliamentController == null ||
                    State.ControllerForUserFee.Value.ReferendumController == null ||
                    State.ControllerForUserFee.Value.RootController == null);
        }
        
        private void CalculateDeveloperFeeController()
        {
            State.ControllerForDeveloperFee.Value = new ControllerForDeveloperFee();
            State.ControllerForDeveloperFee.Value.ParliamentController = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());;
            State.ControllerForDeveloperFee.Value.DeveloperController =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetDeveloperController()
                    .OrganizationCreationInput);
            State.ControllerForDeveloperFee.Value.RootController =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetAssociationControllerForDeveloperFee()
                    .OrganizationCreationInput);
        }

        private void CalculateUserFeeController()
        {
            State.ControllerForUserFee.Value = new ControllerForUserFee();
            State.ControllerForUserFee.Value.ParliamentController = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());;
            State.ControllerForUserFee.Value.ReferendumController =
                State.ReferendumContract.CalculateOrganizationAddress.Call(GetReferendumControllerForUserFee()
                    .OrganizationCreationInput);
            State.ControllerForUserFee.Value.RootController =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetAssociationControllerForUserFee()
                    .OrganizationCreationInput);
        }
        private void CreateReferendumControllerForUserFee()
        {
            State.ReferendumContract.CreateOrganizationBySystemContract.Send(GetReferendumControllerForUserFee());
        }

        private void CreateAssociationControllerForUserFee()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetAssociationControllerForUserFee());
        }
        
        private void CreateDeveloperController()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetDeveloperController());
        }

        private void CreateAssociationControllerForDeveloperFee()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetAssociationControllerForDeveloperFee());
        }

        #endregion

        #region organization create input
        private Referendum.CreateOrganizationBySystemContractInput GetReferendumControllerForUserFee()
        {
            var parliamentOrg = State.ControllerForUserFee.Value.ParliamentController;
            var whiteList = new List<Address> {parliamentOrg};
            return new Referendum.CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Referendum.CreateOrganizationInput
                {
                    TokenSymbol = GetPrimaryTokenSymbol(new Empty()).Value,
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

        private Association.CreateOrganizationBySystemContractInput GetAssociationControllerForUserFee()
        {
            var parliamentOrg = State.ControllerForUserFee.Value.ParliamentController;
            var proposers = new List<Address>
                {State.ControllerForUserFee.Value.ReferendumController, parliamentOrg};
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
                        Proposers = {proposers}
                    }
                }
            };
        }
        private Association.CreateOrganizationBySystemContractInput GetDeveloperController()
        {
            var parliamentOrganization = State.ControllerForDeveloperFee.Value.ParliamentController;
            var proposers = new List<Address> {parliamentOrganization};
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
                        Proposers = {proposers}
                    }
                }
            };
        }

        private Association.CreateOrganizationBySystemContractInput GetAssociationControllerForDeveloperFee()
        {
            var parliamentOrg = State.ControllerForDeveloperFee.Value.ParliamentController;
            var proposers = new List<Address>
            {
                State.ControllerForDeveloperFee.Value.DeveloperController, parliamentOrg
            };
            var actualProposalCount = proposers.Count;
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
        
        #region controller management

        private void AssertControllerForSymbolToPayTxSizeFee()
        {
            if (State.ControllerForSymbolToPayTxFee.Value == null)
            {
                if (State.ParliamentContract.Value == null)
                {
                    State.ParliamentContract.Value =
                        Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
                }
                State.ControllerForSymbolToPayTxFee.Value =
                    State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            }
            Assert(State.ControllerForSymbolToPayTxFee.Value == Context.Sender, "no permission");
        }

        private Address GetControllerForSideRentalParliament()
        {
            if (State.ControllerForSideRentalParliament.Value != null) return State.ControllerForSideRentalParliament.Value;
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }
            State.ControllerForSideRentalParliament.Value =
                State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());

            return State.ControllerForSideRentalParliament.Value;
        }
        #endregion
    }
}
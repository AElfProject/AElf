
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

        private void InitializeAuthorizedController()
        {
            if (State.SideChainCreator.Value == null) return;
            if (State.AssociationContract.Value == null)
            {
                State.AssociationContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.AssociationContractSystemName);
            }
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetControllerCreateInputForSideChainRental());
        }

        public override Empty ChangeSymbolsToPayTXSizeFeeController(Address input)
        {
            AssertControllerForSymbolToPayTxSizeFee();
            Assert(input != null, "invalid input");
            var isNewControllerIsExist = State.ParliamentContract.ValidateOrganizationExist.Call(input);
            Assert(isNewControllerIsExist.Value, "new controller does not exist");
            State.SymbolToPayTxFeeController.Value = input;
            return new Empty();
        }
        
        public override Empty ChangeSideChainParliamentController(Address input)
        {
            AssertControllerForSideChainRental();
            Assert(input != null, "invalid input");
            var isNewControllerIsExist = State.ParliamentContract.ValidateOrganizationExist.Call(input);
            Assert(isNewControllerIsExist.Value, "new controller does not exist");
            State.SideRentalParliamentController.Value = input;
            return new Empty();
        }

        public override Empty ChangeCrossChainTokenContractRegistrationController(Address input)
        {
            CheckCrossChainTokenContractRegistrationControllerAuthority();
            State.CrossChainTokenContractRegistrationController.Value = input;
            return new Empty();
        }

        public override Address GetCrossChainTokenContractRegistrationController(Empty input)
        {
            var controller = GetCrossChainTokenContractRegistrationController();
            return controller;
        }

        private bool ControllersInitialized()
        {
            if(State.DeveloperFeeController.Value == null)
                State.DeveloperFeeController.Value = new ControllerForDeveloperFee();
            if(State.UserFeeController.Value == null)
                State.UserFeeController.Value = new ControllerForUserFee();
            return !(State.DeveloperFeeController.Value.DeveloperController == null ||
                    State.DeveloperFeeController.Value.ParliamentController == null ||
                    State.DeveloperFeeController.Value.RootController == null ||
                    State.UserFeeController.Value.ParliamentController == null ||
                    State.UserFeeController.Value.ReferendumController == null ||
                    State.UserFeeController.Value.RootController == null);
        }
        
        private void CalculateDeveloperFeeController()
        {
            State.DeveloperFeeController.Value = new ControllerForDeveloperFee();
            State.DeveloperFeeController.Value.ParliamentController = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());;
            State.DeveloperFeeController.Value.DeveloperController =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetDeveloperControllerCreateInput()
                    .OrganizationCreationInput);
            State.DeveloperFeeController.Value.RootController =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetAssociationControllerCreateInputForDeveloperFee()
                    .OrganizationCreationInput);
        }

        private void CalculateUserFeeController()
        {
            State.UserFeeController.Value = new ControllerForUserFee();
            State.UserFeeController.Value.ParliamentController = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());;
            State.UserFeeController.Value.ReferendumController =
                State.ReferendumContract.CalculateOrganizationAddress.Call(GetReferendumControllerCreateInputForUserFee()
                    .OrganizationCreationInput);
            State.UserFeeController.Value.RootController =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetAssociationControllerCreateInputForUserFee()
                    .OrganizationCreationInput);
        }
        private void CreateReferendumControllerForUserFee()
        {
            State.ReferendumContract.CreateOrganizationBySystemContract.Send(GetReferendumControllerCreateInputForUserFee());
        }

        private void CreateAssociationControllerForUserFee()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetAssociationControllerCreateInputForUserFee());
        }
        
        private void CreateDeveloperController()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetDeveloperControllerCreateInput());
        }

        private void CreateAssociationControllerForDeveloperFee()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetAssociationControllerCreateInputForDeveloperFee());
        }

        #endregion

        #region organization create input
        private Referendum.CreateOrganizationBySystemContractInput GetReferendumControllerCreateInputForUserFee()
        {
            var parliamentOrg = State.UserFeeController.Value.ParliamentController;
            var whiteList = new List<Address> {parliamentOrg};
            var tokenSymbol = GetPrimaryTokenSymbol(new Empty()).Value;
            return new Referendum.CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Referendum.CreateOrganizationInput
                {
                    TokenSymbol = tokenSymbol,
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

        private Association.CreateOrganizationBySystemContractInput GetAssociationControllerCreateInputForUserFee()
        {
            var parliamentOrg = State.UserFeeController.Value.ParliamentController;
            var proposers = new List<Address>
                {State.UserFeeController.Value.ReferendumController, parliamentOrg};
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
        private Association.CreateOrganizationBySystemContractInput GetDeveloperControllerCreateInput()
        {
            var parliamentOrganization = State.DeveloperFeeController.Value.ParliamentController;
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

        private Association.CreateOrganizationBySystemContractInput GetAssociationControllerCreateInputForDeveloperFee()
        {
            var parliamentOrg = State.DeveloperFeeController.Value.ParliamentController;
            var proposers = new List<Address>
            {
                State.DeveloperFeeController.Value.DeveloperController, parliamentOrg
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
        
        private Association.CreateOrganizationBySystemContractInput GetControllerCreateInputForSideChainRental()
        {
            var sideChainCreator = State.SideChainCreator.Value;
            var parliamentAddress = GetControllerForSideRentalParliament();
            var proposers = new List<Address> {parliamentAddress, sideChainCreator};
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
        #endregion
        
        #region controller management

        private void AssertControllerForSymbolToPayTxSizeFee()
        {
            if (State.SymbolToPayTxFeeController.Value == null)
            {
                if (State.ParliamentContract.Value == null)
                {
                    State.ParliamentContract.Value =
                        Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
                }
                State.SymbolToPayTxFeeController.Value =
                    State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            }
            Assert(State.SymbolToPayTxFeeController.Value == Context.Sender, "no permission");
        }

        private Address GetControllerForSideRentalParliament()
        {
            if (State.SideRentalParliamentController.Value == null)
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
                State.SideRentalParliamentController.Value =
                    State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            }
            
            return State.SideRentalParliamentController.Value;
        }
        
        private void AssertDeveloperFeeController()
        {
            if (State.DeveloperFeeController.Value == null)
            {
                InitializeDeveloperFeeController();
            }
            Assert(Context.Sender == State.DeveloperFeeController.Value.RootController, "no permission");
        }

        private void InitializeDeveloperFeeController()
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
                
            State.DeveloperFeeController.Value = new ControllerForDeveloperFee();
            State.DeveloperFeeController.Value.ParliamentController = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());;
            State.DeveloperFeeController.Value.DeveloperController =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetDeveloperControllerCreateInput()
                    .OrganizationCreationInput);
            State.DeveloperFeeController.Value.RootController =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetAssociationControllerCreateInputForDeveloperFee()
                    .OrganizationCreationInput);
            CreateDeveloperController();
            CreateAssociationControllerForDeveloperFee();
        }

        private void AssertUserFeeController()
        {
            if (State.UserFeeController.Value == null)
            {
                InitializeUserFeeController();
            }
            Assert(Context.Sender == State.UserFeeController.Value.RootController, "no permission");
        }

        private void InitializeUserFeeController()
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
            State.UserFeeController.Value = new ControllerForUserFee();
            State.UserFeeController.Value.ParliamentController = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());;
            State.UserFeeController.Value.ReferendumController =
                State.ReferendumContract.CalculateOrganizationAddress.Call(GetReferendumControllerCreateInputForUserFee()
                    .OrganizationCreationInput);
            State.UserFeeController.Value.RootController =
                State.AssociationContract.CalculateOrganizationAddress.Call(GetAssociationControllerCreateInputForUserFee()
                    .OrganizationCreationInput);
            CreateReferendumControllerForUserFee();
            CreateAssociationControllerForUserFee();
        }
        #endregion
    }
}
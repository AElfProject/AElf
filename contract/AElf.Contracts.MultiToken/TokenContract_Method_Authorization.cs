using System.Collections.Generic;
using Acs1;
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

            State.AssociationContract.CreateOrganizationBySystemContract.Send(
                GetControllerCreateInputForSideChainRental());
        }

        public override Empty ChangeSymbolsToPayTXSizeFeeController(AuthorityInfo input)
        {
            AssertControllerForSymbolToPayTxSizeFee();
            Assert(input != null, "invalid input");
            Assert(input.ContractAddress == State.ParliamentContract.Value, "wrong organization type");
            var isNewControllerIsExist = State.ParliamentContract.ValidateOrganizationExist.Call(input.OwnerAddress);
            Assert(isNewControllerIsExist.Value, "new controller does not exist");
            State.SymbolToPayTxFeeController.Value = input;
            return new Empty();
        }

        public override Empty ChangeSideChainParliamentController(AuthorityInfo input)
        {
            AssertControllerForSideChainRental();
            Assert(input != null, "invalid input");
            Assert(input.ContractAddress == State.ParliamentContract.Value, "wrong organization type");
            var isNewControllerIsExist = State.ParliamentContract.ValidateOrganizationExist.Call(input.OwnerAddress);
            Assert(isNewControllerIsExist.Value, "new controller does not exist");
            State.SideRentalParliamentController.Value = input;
            return new Empty();
        }

        public override Empty ChangeCrossChainTokenContractRegistrationController(AuthorityInfo input)
        {
            CheckCrossChainTokenContractRegistrationControllerAuthority();
            var organizationExist = CheckOrganizationExist(input);
            Assert(organizationExist, "Invalid authority input.");
            State.CrossChainTokenContractRegistrationController.Value = input;
            return new Empty();
        }

        public override AuthorityInfo GetCrossChainTokenContractRegistrationController(Empty input)
        {
            var controller = GetCrossChainTokenContractRegistrationController();
            return controller;
        }

        private void CreateReferendumControllerForUserFee()
        {
            State.ReferendumContract.CreateOrganizationBySystemContract.Send(
                GetReferendumControllerCreateInputForUserFee());
        }

        private void CreateAssociationControllerForUserFee()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(
                GetAssociationControllerCreateInputForUserFee());
        }

        private void CreateDeveloperController()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(GetDeveloperControllerCreateInput());
        }

        private void CreateAssociationControllerForDeveloperFee()
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(
                GetAssociationControllerCreateInputForDeveloperFee());
        }

        #endregion

        #region organization create input

        private Referendum.CreateOrganizationBySystemContractInput GetReferendumControllerCreateInputForUserFee()
        {
            var parliamentOrg = State.UserFeeController.Value.ParliamentController.OwnerAddress;
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
            var parliamentOrg = State.UserFeeController.Value.ParliamentController.OwnerAddress;
            var proposers = new List<Address>
                {State.UserFeeController.Value.ReferendumController.OwnerAddress, parliamentOrg};
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
            var parliamentOrganization = State.DeveloperFeeController.Value.ParliamentController.OwnerAddress;
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
            var parliamentOrg = State.DeveloperFeeController.Value.ParliamentController.OwnerAddress;
            var proposers = new List<Address>
            {
                State.DeveloperFeeController.Value.DeveloperController.OwnerAddress, parliamentOrg
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
                InitializeSymbolToPayTxFeeController();
            }

            Assert(State.SymbolToPayTxFeeController.Value.OwnerAddress == Context.Sender, "no permission");
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

                State.SideRentalParliamentController.Value = new AuthorityInfo
                {
                    ContractAddress = State.ParliamentContract.Value,
                    OwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty())
                };
            }

            return State.SideRentalParliamentController.Value.OwnerAddress;
        }

        private void AssertDeveloperFeeController()
        {
            if (State.DeveloperFeeController.Value == null)
            {
                InitializeDeveloperFeeController();
            }

            Assert(Context.Sender == State.DeveloperFeeController.Value.RootController.OwnerAddress, "no permission");
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

            State.DeveloperFeeController.Value = new DeveloperFeeController
            {
                ParliamentController = new AuthorityInfo(),
                DeveloperController = new AuthorityInfo(),
                RootController = new AuthorityInfo()
            };
            State.DeveloperFeeController.Value.ParliamentController.OwnerAddress =
                State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            State.DeveloperFeeController.Value.ParliamentController.ContractAddress = State.ParliamentContract.Value;
            State.DeveloperFeeController.Value.DeveloperController.ContractAddress = State.AssociationContract.Value;
            State.DeveloperFeeController.Value.DeveloperController.OwnerAddress = State.AssociationContract.CalculateOrganizationAddress.Call(GetDeveloperControllerCreateInput()
                .OrganizationCreationInput);
            State.DeveloperFeeController.Value.RootController.ContractAddress = State.AssociationContract.Value;
            State.DeveloperFeeController.Value.RootController.OwnerAddress = State.AssociationContract.CalculateOrganizationAddress.Call(
                GetAssociationControllerCreateInputForDeveloperFee()
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

            Assert(Context.Sender == State.UserFeeController.Value.RootController.OwnerAddress, "no permission");
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
            
            State.UserFeeController.Value = new UserFeeController
            {
                RootController = new AuthorityInfo(),
                ParliamentController = new AuthorityInfo(),
                ReferendumController = new AuthorityInfo()
            };
            State.UserFeeController.Value.ParliamentController.ContractAddress = State.ParliamentContract.Value;
            State.UserFeeController.Value.ParliamentController.OwnerAddress =
                State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());

            State.UserFeeController.Value.ReferendumController.ContractAddress = State.ReferendumContract.Value;
            State.UserFeeController.Value.ReferendumController.OwnerAddress = State.ReferendumContract.CalculateOrganizationAddress.Call(
                GetReferendumControllerCreateInputForUserFee()
                    .OrganizationCreationInput);
            State.UserFeeController.Value.RootController.ContractAddress = State.AssociationContract.Value;
            State.UserFeeController.Value.RootController.OwnerAddress = State.AssociationContract.CalculateOrganizationAddress.Call(
                GetAssociationControllerCreateInputForUserFee()
                    .OrganizationCreationInput);
            CreateReferendumControllerForUserFee();
            CreateAssociationControllerForUserFee();
        }

        private void InitializeSymbolToPayTxFeeController()
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            State.SymbolToPayTxFeeController.Value = new AuthorityInfo
            {
                ContractAddress = State.ParliamentContract.Value,
                OwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty())
            };
        }

        #endregion
    }
}
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
            var defaultUserFeeController = GetDefaultUserFeeController();
            CreateReferendumControllerForUserFee(defaultUserFeeController.ParliamentController.OwnerAddress);
            CreateAssociationControllerForUserFee(defaultUserFeeController.ParliamentController.OwnerAddress,
                defaultUserFeeController.ReferendumController.OwnerAddress);
            State.UserFeeController.Value = defaultUserFeeController;
            
            var developerController = GetDefaultDeveloperFeeController();
            CreateDeveloperController(developerController.ParliamentController.OwnerAddress);
            CreateAssociationControllerForDeveloperFee(developerController.ParliamentController.OwnerAddress,
                developerController.DeveloperController.OwnerAddress);
            State.DeveloperFeeController.Value = developerController;
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
            Assert(CheckOrganizationExist(input), "new controller does not exist");
            State.SymbolToPayTxFeeController.Value = input;
            return new Empty();
        }

        public override Empty ChangeSideChainParliamentController(AuthorityInfo input)
        {
            AssertControllerForSideChainRental();
            Assert(input != null, "invalid input");
            Assert(input.ContractAddress == State.ParliamentContract.Value, "wrong organization type");
            Assert(CheckOrganizationExist(input), "new controller does not exist");
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
            if (State.CrossChainTokenContractRegistrationController.Value == null)
            {
                return GetCrossChainTokenContractRegistrationController();
            }
            return State.CrossChainTokenContractRegistrationController.Value;
        }

        private void CreateReferendumControllerForUserFee(Address parliamentAddress)
        {
            State.ReferendumContract.CreateOrganizationBySystemContract.Send(
                GetReferendumControllerCreateInputForUserFee(parliamentAddress));
        }

        private void CreateAssociationControllerForUserFee(Address parliamentAddress, Address referendumAddress)
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(
                GetAssociationControllerCreateInputForUserFee(parliamentAddress, referendumAddress));
        }

        private void CreateDeveloperController(Address parliamentAddress)
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(
                GetDeveloperControllerCreateInput(parliamentAddress));
        }

        private void CreateAssociationControllerForDeveloperFee(Address parliamentAddress, Address developerAddress)
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(
                GetAssociationControllerCreateInputForDeveloperFee(parliamentAddress, developerAddress));
        }

        #endregion

        #region organization create input

        private Referendum.CreateOrganizationBySystemContractInput GetReferendumControllerCreateInputForUserFee(
            Address parliamentAddress)
        {
            var whiteList = new List<Address> {parliamentAddress};
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

        private Association.CreateOrganizationBySystemContractInput GetAssociationControllerCreateInputForUserFee(
            Address parliamentAddress, Address referendumAddress)
        {
            var proposers = new List<Address>
                {referendumAddress, parliamentAddress};
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

        private Association.CreateOrganizationBySystemContractInput GetDeveloperControllerCreateInput(
            Address parliamentAddress)
        {
            var proposers = new List<Address> {parliamentAddress};
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

        private Association.CreateOrganizationBySystemContractInput GetAssociationControllerCreateInputForDeveloperFee(
            Address parliamentAddress, Address developerAddress)
        {
            var proposers = new List<Address>
            {
                developerAddress, parliamentAddress
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
                State.SymbolToPayTxFeeController.Value = GetDefaultSymbolToPayTxFeeController();
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
            var controller = State.DeveloperFeeController.Value;
            if ( controller== null)
            {
                controller = GetDefaultDeveloperFeeController();
            }

            Assert(Context.Sender == controller.RootController.OwnerAddress, "no permission");
        }

        private DeveloperFeeController GetDefaultDeveloperFeeController()
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

            var developerFeeController = new DeveloperFeeController
            {
                ParliamentController = new AuthorityInfo(),
                DeveloperController = new AuthorityInfo(),
                RootController = new AuthorityInfo()
            };
            developerFeeController.ParliamentController.OwnerAddress =
                State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            developerFeeController.ParliamentController.ContractAddress = State.ParliamentContract.Value;
            developerFeeController.DeveloperController.ContractAddress = State.AssociationContract.Value;
            developerFeeController.DeveloperController.OwnerAddress =
                State.AssociationContract.CalculateOrganizationAddress.Call(
                    GetDeveloperControllerCreateInput(developerFeeController.ParliamentController.OwnerAddress)
                        .OrganizationCreationInput);
            developerFeeController.RootController.ContractAddress = State.AssociationContract.Value;
            developerFeeController.RootController.OwnerAddress =
                State.AssociationContract.CalculateOrganizationAddress.Call(
                    GetAssociationControllerCreateInputForDeveloperFee(
                            developerFeeController.ParliamentController.OwnerAddress,
                            developerFeeController.DeveloperController.OwnerAddress)
                        .OrganizationCreationInput);
            return developerFeeController;
        }

        private void AssertUserFeeController()
        {
            var controller = State.UserFeeController.Value;
            if (controller == null)
            {
                controller = GetDefaultUserFeeController();
            }

            Assert(Context.Sender == controller.RootController.OwnerAddress, "no permission");
        }

        private UserFeeController GetDefaultUserFeeController()
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

            var userFeeController = new UserFeeController
            {
                RootController = new AuthorityInfo(),
                ParliamentController = new AuthorityInfo(),
                ReferendumController = new AuthorityInfo()
            };
            userFeeController.ParliamentController.ContractAddress = State.ParliamentContract.Value;
            userFeeController.ParliamentController.OwnerAddress =
                State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());

            userFeeController.ReferendumController.ContractAddress = State.ReferendumContract.Value;
            userFeeController.ReferendumController.OwnerAddress =
                State.ReferendumContract.CalculateOrganizationAddress.Call(
                    GetReferendumControllerCreateInputForUserFee(userFeeController.ParliamentController.OwnerAddress)
                        .OrganizationCreationInput);
            userFeeController.RootController.ContractAddress = State.AssociationContract.Value;
            userFeeController.RootController.OwnerAddress = State.AssociationContract.CalculateOrganizationAddress.Call(
                GetAssociationControllerCreateInputForUserFee(userFeeController.ParliamentController.OwnerAddress,
                        userFeeController.ReferendumController.OwnerAddress)
                    .OrganizationCreationInput);
            return userFeeController;
        }

        private AuthorityInfo GetDefaultSymbolToPayTxFeeController()
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            return new AuthorityInfo
            {
                ContractAddress = State.ParliamentContract.Value,
                OwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty())
            };
        }

        #endregion
    }
}
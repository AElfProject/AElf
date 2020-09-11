using System.Collections.Generic;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
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
            var defaultParliamentController = GetDefaultParliamentController();
            if (State.UserFeeController.Value == null)
            {
                var defaultUserFeeController = GetDefaultUserFeeController(defaultParliamentController);
                CreateReferendumControllerForUserFee(defaultParliamentController.OwnerAddress);
                CreateAssociationControllerForUserFee(defaultParliamentController.OwnerAddress,
                    defaultUserFeeController.ReferendumController.OwnerAddress);
                State.UserFeeController.Value = defaultUserFeeController;
            }

            if (State.DeveloperFeeController.Value == null)
            {
                var developerController = GetDefaultDeveloperFeeController(defaultParliamentController);
                CreateDeveloperController(defaultParliamentController.OwnerAddress);
                CreateAssociationControllerForDeveloperFee(defaultParliamentController.OwnerAddress,
                    developerController.DeveloperController.OwnerAddress);
                State.DeveloperFeeController.Value = developerController;
            }
            
            if (State.SideChainCreator.Value == null || State.SideChainRentalController.Value != null) return new Empty();
            var sideChainRentalController = GetDefaultSideChainRentalController(defaultParliamentController);
            CreateAssociationControllerForSideChainRental(State.SideChainCreator.Value, defaultParliamentController.OwnerAddress);
            State.SideChainRentalController.Value = sideChainRentalController;
            return new Empty();
        }

        public override Empty ChangeSymbolsToPayTXSizeFeeController(AuthorityInfo input)
        {
            AssertControllerForSymbolToPayTxSizeFee();
            Assert(CheckOrganizationExist(input), "new controller does not exist");
            State.SymbolToPayTxFeeController.Value = input;
            return new Empty();
        }

        public override Empty ChangeSideChainRentalController(AuthorityInfo input)
        {
            AssertControllerForSideChainRental();
            Assert(CheckOrganizationExist(input), "new controller does not exist");
            State.SideChainRentalController.Value = input;
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
        
        public override Empty ChangeUserFeeController(AuthorityInfo input)
        {
            AssertUserFeeController();
            Assert(CheckOrganizationExist(input), "Invalid authority input.");
            State.UserFeeController.Value.RootController = input;
            State.UserFeeController.Value.ParliamentController = null;
            State.UserFeeController.Value.ReferendumController = null;
            return new Empty();
        }
        
        public override Empty ChangeDeveloperController(AuthorityInfo input)
        {
            AssertDeveloperFeeController();
            Assert(CheckOrganizationExist(input), "Invalid authority input.");
            State.DeveloperFeeController.Value.RootController = input;
            State.DeveloperFeeController.Value.ParliamentController = null;
            State.DeveloperFeeController.Value.DeveloperController = null;
            return new Empty();
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
        private void CreateAssociationControllerForSideChainRental(Address sideChainCreator, Address parliamentAddress)
        {
            State.AssociationContract.CreateOrganizationBySystemContract.Send(
                GetControllerCreateInputForSideChainRental(sideChainCreator, parliamentAddress));
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

        private Association.CreateOrganizationBySystemContractInput GetControllerCreateInputForSideChainRental(
            Address sideChainCreator, Address parliamentAddress)
        {
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
        private AuthorityInfo GetDefaultParliamentController()
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
        
        private DeveloperFeeController GetDefaultDeveloperFeeController(AuthorityInfo defaultParliamentController)
        {
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
            developerFeeController.ParliamentController = defaultParliamentController;
            developerFeeController.DeveloperController.ContractAddress = State.AssociationContract.Value;
            developerFeeController.DeveloperController.OwnerAddress =
                State.AssociationContract.CalculateOrganizationAddress.Call(
                    GetDeveloperControllerCreateInput(defaultParliamentController.OwnerAddress)
                        .OrganizationCreationInput);
            developerFeeController.RootController.ContractAddress = State.AssociationContract.Value;
            developerFeeController.RootController.OwnerAddress =
                State.AssociationContract.CalculateOrganizationAddress.Call(
                    GetAssociationControllerCreateInputForDeveloperFee(
                            defaultParliamentController.OwnerAddress,
                            developerFeeController.DeveloperController.OwnerAddress)
                        .OrganizationCreationInput);
            return developerFeeController;
        }
        
        private UserFeeController GetDefaultUserFeeController(AuthorityInfo defaultParliamentController)
        {
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
            userFeeController.ParliamentController = defaultParliamentController;
            userFeeController.ReferendumController.ContractAddress = State.ReferendumContract.Value;
            userFeeController.ReferendumController.OwnerAddress =
                State.ReferendumContract.CalculateOrganizationAddress.Call(
                    GetReferendumControllerCreateInputForUserFee(defaultParliamentController.OwnerAddress)
                        .OrganizationCreationInput);
            userFeeController.RootController.ContractAddress = State.AssociationContract.Value;
            userFeeController.RootController.OwnerAddress = State.AssociationContract.CalculateOrganizationAddress.Call(
                GetAssociationControllerCreateInputForUserFee(defaultParliamentController.OwnerAddress,
                        userFeeController.ReferendumController.OwnerAddress)
                    .OrganizationCreationInput);
            return userFeeController;
        }

        private AuthorityInfo GetDefaultSymbolToPayTxFeeController()
        {
            return GetDefaultParliamentController();
        }
        
        private AuthorityInfo GetDefaultSideChainRentalController(AuthorityInfo defaultParliamentController)
        {
            if (State.AssociationContract.Value == null)
            {
                State.AssociationContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.AssociationContractSystemName);
            }
            var calculatedAddress = State.AssociationContract.CalculateOrganizationAddress.Call(
                GetControllerCreateInputForSideChainRental(
                            State.SideChainCreator.Value,
                            defaultParliamentController.OwnerAddress)
                        .OrganizationCreationInput);
            return new AuthorityInfo
            {
                ContractAddress = State.AssociationContract.Value,
                OwnerAddress = calculatedAddress
            };
        }
        
        private void AssertDeveloperFeeController()
        {
            Assert(State.DeveloperFeeController.Value != null,
                "controller does not initialize, call InitializeAuthorizedController first");

            Assert(Context.Sender == State.DeveloperFeeController.Value.RootController.OwnerAddress, "no permission");
        }
        
        private void AssertUserFeeController()
        {
            Assert(State.UserFeeController.Value != null,
                "controller does not initialize, call InitializeAuthorizedController first");
            // ReSharper disable once PossibleNullReferenceException
            Assert(Context.Sender == State.UserFeeController.Value.RootController.OwnerAddress, "no permission");
        }
        
        private void AssertControllerForSymbolToPayTxSizeFee()
        {
            if (State.SymbolToPayTxFeeController.Value == null)
            {
                State.SymbolToPayTxFeeController.Value = GetDefaultSymbolToPayTxFeeController();
            }

            Assert(State.SymbolToPayTxFeeController.Value.OwnerAddress == Context.Sender, "no permission");
        }
        
        private void AssertControllerForSideChainRental()
        {
            Assert(State.SideChainRentalController.Value != null, "controller does not initialize, call InitializeAuthorizedController first");
            // ReSharper disable once PossibleNullReferenceException
            Assert(State.SideChainRentalController.Value.OwnerAddress == Context.Sender, "no permission");
        }

        #endregion
    }
}
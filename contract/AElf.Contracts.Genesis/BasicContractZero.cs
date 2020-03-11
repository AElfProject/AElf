using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Acs0;
using Acs1;
using Acs3;

namespace AElf.Contracts.Genesis
{
    public partial class BasicContractZero : BasicContractZeroContainer.BasicContractZeroBase
    {
        #region Views

        public override UInt64Value CurrentContractSerialNumber(Empty input)
        {
            return new UInt64Value() {Value = State.ContractSerialNumber.Value};
        }

        public override ContractInfo GetContractInfo(Address input)
        {
            var info = State.ContractInfos[input];
            if (info == null)
            {
                return new ContractInfo();
            }

            return info;
        }

        public override Address GetContractAuthor(Address input)
        {
            var info = State.ContractInfos[input];
            return info?.Author;
        }

        public override Hash GetContractHash(Address input)
        {
            var info = State.ContractInfos[input];
            return info?.CodeHash;
        }

        public override Address GetContractAddressByName(Hash input)
        {
            var address = State.NameAddressMapping[input];
            return address;
        }

        public override SmartContractRegistration GetSmartContractRegistrationByAddress(Address input)
        {
            var info = State.ContractInfos[input];
            if (info == null)
            {
                return null;
            }

            return State.SmartContractRegistrations[info.CodeHash];
        }
        
        public override SmartContractRegistration GetSmartContractRegistration(Hash input)
        {
            return State.SmartContractRegistrations[input];
        }

        public override Empty ValidateSystemContractAddress(ValidateSystemContractAddressInput input)
        {
            var actualAddress = GetContractAddressByName(input.SystemContractHashName);
            Assert(actualAddress == input.Address, "Address not expected.");
            return new Empty();
        }

        public override AuthorityInfo GetContractDeploymentController(Empty input)
        {
            return State.ContractDeploymentController.Value;
        }

        public override AuthorityInfo GetCodeCheckController(Empty input)
        {
            return State.CodeCheckController.Value;
        }

        #endregion Views

        #region Actions

        public override Address DeploySystemSmartContract(SystemContractDeploymentInput input)
        {
            Assert(!State.Initialized.Value || !State.ContractDeploymentAuthorityRequired.Value,
                "System contract deployment failed.");
            RequireSenderAuthority();
            var name = input.Name;
            var category = input.Category;
            var code = input.Code.ToByteArray();
            var transactionMethodCallList = input.TransactionMethodCallList;

            // Context.Sender should be identical to Genesis contract address before initialization in production
            var address = DeploySmartContract(name, category, code, true, Context.Sender);

            if (transactionMethodCallList != null)
            {
                foreach (var methodCall in transactionMethodCallList.Value)
                {
                    Context.SendInline(address, methodCall.MethodName, methodCall.Params);
                }
            }

            return address;
        }

        public override Hash ProposeNewContract(ContractDeploymentInput input)
        {
            // AssertDeploymentProposerAuthority(Context.Sender);
            var proposedContractInputHash = CalculateHashFromInput(input);
            RegisterContractProposingData(proposedContractInputHash);

            // Create proposal for deployment
            var proposalCreationInput = new CreateProposalBySystemContractInput
            {
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = Context.Self,
                    ContractMethodName =
                        nameof(BasicContractZeroContainer.BasicContractZeroBase.ProposeContractCodeCheck),
                    Params = new ContractCodeCheckInput
                    {
                        ContractInput = input.ToByteString(),
                        CodeCheckReleaseMethod = nameof(DeploySmartContract),
                        ProposedContractInputHash = proposedContractInputHash
                    }.ToByteString(),
                    OrganizationAddress = State.ContractDeploymentController.Value.OwnerAddress,
                    ExpiredTime = Context.CurrentBlockTime.AddSeconds(ContractProposalExpirationTimePeriod)
                },
                OriginProposer = Context.Sender
            };
            Context.SendInline(State.ContractDeploymentController.Value.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState
                    .CreateProposalBySystemContract), proposalCreationInput.ToByteString());

            Context.Fire(new ContractProposed
            {
                ProposedContractInputHash = proposedContractInputHash
            });

            return proposedContractInputHash;
        }

        public override Hash ProposeUpdateContract(ContractUpdateInput input)
        {
            var proposedContractInputHash = CalculateHashFromInput(input);
            RegisterContractProposingData(proposedContractInputHash);

            var contractAddress = input.Address;
            var info = State.ContractInfos[contractAddress];
            Assert(info != null, "Contract not found.");
            AssertAuthorityByContractInfo(info, Context.Sender);

            // Create proposal for contract update
            var proposalCreationInput = new CreateProposalBySystemContractInput
            {
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = Context.Self,
                    ContractMethodName =
                        nameof(BasicContractZeroContainer.BasicContractZeroBase.ProposeContractCodeCheck),
                    Params = new ContractCodeCheckInput
                    {
                        ContractInput = input.ToByteString(),
                        CodeCheckReleaseMethod = nameof(UpdateSmartContract),
                        ProposedContractInputHash = proposedContractInputHash
                    }.ToByteString(),
                    OrganizationAddress = State.ContractDeploymentController.Value.OwnerAddress,
                    ExpiredTime = Context.CurrentBlockTime.AddSeconds(ContractProposalExpirationTimePeriod)
                },
                OriginProposer = Context.Sender
            };
            Context.SendInline(State.ContractDeploymentController.Value.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState
                    .CreateProposalBySystemContract), proposalCreationInput);

            Context.Fire(new ContractProposed
            {
                ProposedContractInputHash = proposedContractInputHash
            });

            return proposedContractInputHash;
        }

        public override Hash ProposeContractCodeCheck(ContractCodeCheckInput input)
        {
            RequireSenderAuthority(State.ContractDeploymentController.Value.OwnerAddress);
            AssertCodeCheckProposingInput(input);
            var proposedContractInputHash = input.ProposedContractInputHash;
            var proposedInfo = State.ContractProposingInputMap[proposedContractInputHash];
            Assert(proposedInfo != null && proposedInfo.Status == ContractProposingInputStatus.Approved,
                "Invalid contract proposing status.");
            proposedInfo.Status = ContractProposingInputStatus.CodeCheckProposed;
            State.ContractProposingInputMap[proposedContractInputHash] = proposedInfo;

            var codeCheckController = State.CodeCheckController.Value;
            var proposalCreationInput = new CreateProposalBySystemContractInput
            {
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = Context.Self,
                    ContractMethodName = input.CodeCheckReleaseMethod,
                    Params = input.ContractInput,
                    OrganizationAddress = codeCheckController.OwnerAddress,
                    ExpiredTime = Context.CurrentBlockTime.AddSeconds(CodeCheckProposalExpirationTimePeriod)
                },
                OriginProposer = proposedInfo.Proposer
            };

            proposedInfo.ExpiredTime = proposalCreationInput.ProposalInput.ExpiredTime;
            State.ContractProposingInputMap[proposedContractInputHash] = proposedInfo;
            Context.SendInline(codeCheckController.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState
                    .CreateProposalBySystemContract), proposalCreationInput);

            // Fire event to trigger BPs checking contract code
            Context.Fire(new CodeCheckRequired
            {
                Code = ExtractCodeFromContractCodeCheckInput(input),
                ProposedContractInputHash = proposedContractInputHash
            });

            return proposedContractInputHash;
        }

        public override Empty ReleaseApprovedContract(ReleaseContractInput input)
        {
            var contractProposingInput = State.ContractProposingInputMap[input.ProposedContractInputHash];
            Assert(
                contractProposingInput != null &&
                contractProposingInput.Status == ContractProposingInputStatus.Proposed &&
                contractProposingInput.Proposer == Context.Sender, "Invalid contract proposing status.");
            contractProposingInput.Status = ContractProposingInputStatus.Approved;
            State.ContractProposingInputMap[input.ProposedContractInputHash] = contractProposingInput;
            Context.SendInline(State.ContractDeploymentController.Value.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.Release),
                input.ProposalId.ToByteString());
            return new Empty();
        }

        public override Empty ReleaseCodeCheckedContract(ReleaseContractInput input)
        {
            var contractProposingInput = State.ContractProposingInputMap[input.ProposedContractInputHash];

            Assert(
                contractProposingInput != null &&
                contractProposingInput.Status == ContractProposingInputStatus.CodeCheckProposed &&
                contractProposingInput.Proposer == Context.Sender, "Invalid contract proposing status.");
            contractProposingInput.Status = ContractProposingInputStatus.CodeChecked;
            State.ContractProposingInputMap[input.ProposedContractInputHash] = contractProposingInput;
            var codeCheckController = State.CodeCheckController.Value;
            Context.SendInline(codeCheckController.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.Release), input.ProposalId);
            return new Empty();
        }


        public override Address DeploySmartContract(ContractDeploymentInput input)
        {
            RequireSenderAuthority(State.CodeCheckController.Value?.OwnerAddress);
            // AssertDeploymentProposerAuthority(Context.Origin);

            var inputHash = CalculateHashFromInput(input);
            TryClearContractProposingData(inputHash, out var contractProposingInput);

            var address =
                DeploySmartContract(null, input.Category, input.Code.ToByteArray(), false,
                    DecideNonSystemContractAuthor(contractProposingInput?.Proposer, Context.Sender));
            return address;
        }

        public override Address UpdateSmartContract(ContractUpdateInput input)
        {
            var contractAddress = input.Address;
            var code = input.Code.ToByteArray();
            var info = State.ContractInfos[contractAddress];
            Assert(info != null, "Contract not found.");
            RequireSenderAuthority(State.CodeCheckController.Value?.OwnerAddress);
            var inputHash = CalculateHashFromInput(input);

            if (!TryClearContractProposingData(inputHash, out _))
                Assert(Context.Sender == info.Author, "No permission.");

            var oldCodeHash = info.CodeHash;
            var newCodeHash = Hash.FromRawBytes(code);
            Assert(!oldCodeHash.Equals(newCodeHash), "Code is not changed.");
            
            Assert(State.SmartContractRegistrations[newCodeHash] == null, "Same code has been deployed before.");

            info.CodeHash = newCodeHash;
            info.Version++;
            State.ContractInfos[contractAddress] = info;

            var reg = new SmartContractRegistration
            {
                Category = info.Category,
                Code = ByteString.CopyFrom(code),
                CodeHash = newCodeHash,
                IsSystemContract = info.IsSystemContract,
                Version = info.Version
            };

            State.SmartContractRegistrations[reg.CodeHash] = reg;

            Context.UpdateContract(contractAddress, reg, null);

            Context.Fire(new CodeUpdated()
            {
                Address = contractAddress,
                OldCodeHash = oldCodeHash,
                NewCodeHash = newCodeHash,
                Version = info.Version
            });

            Context.LogDebug(() => "BasicContractZero - update success: " + contractAddress.GetFormatted());
            return contractAddress;
        }

        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Contract zero already initialized.");
            Assert(Context.Sender == Context.Self, "No permission.");
            State.ContractDeploymentAuthorityRequired.Value = input.ContractDeploymentAuthorityRequired;
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty SetInitialControllerAddress(Address input)
        {
            Assert(State.ContractDeploymentController.Value == null && State.CodeCheckController.Value == null,
                "Genesis owner already initialized");
            var parliamentContractAddress =
                GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            Assert(Context.Sender.Equals(parliamentContractAddress), "Unauthorized to initialize genesis contract.");
            Assert(input != null, "Genesis Owner should not be null.");
            var defaultAuthority = new AuthorityInfo
            {
                OwnerAddress = input,
                ContractAddress = parliamentContractAddress
            };
            State.ContractDeploymentController.Value = defaultAuthority;
            State.CodeCheckController.Value = defaultAuthority;
            return new Empty();
        }

        public override Empty ChangeContractDeploymentController(AuthorityInfo input)
        {
            AssertSenderAddressWith(State.ContractDeploymentController.Value.OwnerAddress);
            var organizationExist = CheckOrganizationExist(input);
            Assert(organizationExist, "Invalid authority input.");
            State.ContractDeploymentController.Value = input;
            return new Empty();
        }

        public override Empty ChangeCodeCheckController(AuthorityInfo input)
        {
            AssertSenderAddressWith(State.CodeCheckController.Value.OwnerAddress);
            RequireParliamentContractAddressSet();
            Assert(State.ParliamentContract.Value == input.ContractAddress && CheckOrganizationExist(input),
                "Invalid authority input.");
            State.CodeCheckController.Value = input;
            return new Empty();
        }

        public override Empty SetContractProposerRequiredState(BoolValue input)
        {
            Assert(!State.Initialized.Value, "Genesis contract already initialized.");
            var address = GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
            Assert(Context.Sender == address, "Unauthorized to set genesis contract state.");

            CreateParliamentOrganizationForInitialControllerAddress(input.Value);
            return new Empty();
        }

        #endregion Actions
    }
}
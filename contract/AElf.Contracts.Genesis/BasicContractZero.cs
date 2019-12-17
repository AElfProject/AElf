using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Acs0;
using Acs3;
using AElf.Contracts.ParliamentAuth;
using InitializeInput = Acs0.InitializeInput;

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

        public override Empty ValidateSystemContractAddress(ValidateSystemContractAddressInput input)
        {
            var actualAddress = GetContractAddressByName(input.SystemContractHashName); 
            Assert(actualAddress == input.Address, "Address not expected.");
            return new Empty();
        }

        public override AddressList GetDeployedContractAddressList(Empty input)
        {
            return State.DeployedContractAddressList.Value;
        }

        #endregion Views

        #region Actions

        public override Address DeploySystemSmartContract(SystemContractDeploymentInput input)
        {
            Assert(!State.Initialized.Value || !State.ContractDeploymentAuthorityRequired.Value,
                "Genesis contract already initialized.");
            RequireSenderAuthority();
            var name = input.Name;
            var category = input.Category;
            var code = input.Code.ToByteArray();
            var transactionMethodCallList = input.TransactionMethodCallList;

            // Context.Sender should be identical to Genesis contract address before initialization in production
            var address = PrivateDeploySystemSmartContract(name, category, code, true, Context.Sender);

            if (transactionMethodCallList != null)
            {
                foreach (var methodCall in transactionMethodCallList.Value)
                {
                    Context.SendInline(address, methodCall.MethodName, methodCall.Params);
                }
            }

            return address;
        }

        private Address PrivateDeploySystemSmartContract(Hash name, int category, byte[] code, bool isSystemContract, Address author)
        {
            if (name != null)
                Assert(State.NameAddressMapping[name] == null, "contract name already been registered");

            var serialNumber = State.ContractSerialNumber.Value;
            // Increment
            State.ContractSerialNumber.Value = serialNumber + 1;
            var contractAddress = AddressHelper.BuildContractAddress(Context.ChainId, serialNumber);

            var codeHash = Hash.FromRawBytes(code);

            var info = new ContractInfo
            {
                SerialNumber = serialNumber,
                Author = author,
                Category = category,
                CodeHash = codeHash,
                IsSystemContract = isSystemContract
            };
            State.ContractInfos[contractAddress] = info;

            var reg = new SmartContractRegistration
            {
                Category = category,
                Code = ByteString.CopyFrom(code),
                CodeHash = codeHash
            };

            State.SmartContractRegistrations[reg.CodeHash] = reg;

            Context.DeployContract(contractAddress, reg, name);

            Context.Fire(new ContractDeployed
            {
                CodeHash = codeHash,
                Address = contractAddress,
                Author = author
            });

            var deployedContractAddressList = State.DeployedContractAddressList.Value;
            if (deployedContractAddressList == null)
            {
                State.DeployedContractAddressList.Value = new AddressList {Value = {contractAddress}};
            }
            else
            {
                deployedContractAddressList.Value.Add(contractAddress);
                State.DeployedContractAddressList.Value = deployedContractAddressList;
            }

            Context.LogDebug(() => "BasicContractZero - Deployment ContractHash: " + codeHash.ToHex());
            Context.LogDebug(() => "BasicContractZero - Deployment success: " + contractAddress.GetFormatted());

            if (name != null)
                State.NameAddressMapping[name] = contractAddress;

            return contractAddress;
        }

        public override Hash ProposeNewContract(ContractDeploymentInput input)
        {
            AssertDeploymentProposerAuthority(Context.Sender);
            var proposedContractInputHash = CalculateHashFromInput(input);
            Assert(State.ContractProposingInputMap[proposedContractInputHash] == null, "Already proposed.");
            State.ContractProposingInputMap[proposedContractInputHash] = new ContractProposingInput
            {
                Proposer = Context.Sender,
                Status = ContractProposingInputStatus.Proposed
            };
            
            RequireParliamentAuthAddressSet();
            
            // Create proposal for deployment
            State.ParliamentAuthContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = Context.Self,
                    ContractMethodName =
                        nameof(BasicContractZeroContainer.BasicContractZeroBase.ProposeContractCodeCheck),
                    Params = new ContractCodeCheckInput
                    {
                        ContractInput = input.ToByteString(),
                        IsContractDeployment = true
                    }.ToByteString(),
                    OrganizationAddress = State.GenesisOwner.Value,
                    ExpiredTime = Context.CurrentBlockTime.AddHours(24) // Maybe, get the interval from configuration
                },
                OriginProposer = Context.Sender
            });
            
            Context.Fire(new ContractProposed
            {
                ProposedContractInputHash = proposedContractInputHash
            });
            
            return proposedContractInputHash;
        }

        public override Hash ProposeUpdateContract(ContractUpdateInput input)
        {
            var proposedContractInputHash = CalculateHashFromInput(input);
            Assert(State.ContractProposingInputMap[proposedContractInputHash] == null, "Already proposed.");
            State.ContractProposingInputMap[proposedContractInputHash] = new ContractProposingInput
            {
                Proposer = Context.Sender,
                Status = ContractProposingInputStatus.Proposed
            };
            
            var contractAddress = input.Address;
            var info = State.ContractInfos[contractAddress];
            Assert(info != null, "Contract does not exist.");
            RequireAuthorityByContractInfo(info);

            // Create proposal for deployment
            RequireParliamentAuthAddressSet();
            State.ParliamentAuthContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = Context.Self,
                    ContractMethodName =
                        nameof(BasicContractZeroContainer.BasicContractZeroBase.ProposeContractCodeCheck),
                    Params = new ContractCodeCheckInput
                    {
                        ContractInput = input.ToByteString(),
                        IsContractDeployment = false
                    }.ToByteString(),
                    OrganizationAddress = State.GenesisOwner.Value,
                    ExpiredTime = Context.CurrentBlockTime.AddMinutes(10) // Maybe, get the interval from configuration
                },
                OriginProposer = Context.Sender
            });

            // Fire event to trigger BPs checking contract code
            Context.Fire(new ContractProposed
            {
                ProposedContractInputHash = proposedContractInputHash
            });
    
            return proposedContractInputHash;
        }
        
        public override Hash ProposeContractCodeCheck(ContractCodeCheckInput input)
        {
            RequireSenderAuthority();
            AssertDeploymentProposerAuthority(Context.Origin);
            var proposedContractInputHash = Hash.FromRawBytes(input.ContractInput.ToByteArray());
            var proposedInfo = State.ContractProposingInputMap[proposedContractInputHash];
            Assert(proposedInfo != null && proposedInfo.Status == ContractProposingInputStatus.Approved,
                "Invalid contract proposing status.");
            proposedInfo.Status = ContractProposingInputStatus.PreCodeChecked;
            State.ContractProposingInputMap[proposedContractInputHash] = proposedInfo;
            
            RequireParliamentAuthAddressSet();
            
            // Create proposal for deployment
            State.ParliamentAuthContract.CreateProposalBySystemContract.Send(new CreateProposalBySystemContractInput
            {
                ProposalInput = new CreateProposalInput
                {
                    ToAddress = Context.Self,
                    ContractMethodName = input.IsContractDeployment
                        ? nameof(BasicContractZeroContainer.BasicContractZeroBase.DeploySmartContract)
                        : nameof(BasicContractZeroContainer.BasicContractZeroBase.UpdateSmartContract),
                    Params = input.ContractInput,
                    OrganizationAddress = State.GenesisOwner.Value,
                    ExpiredTime = Context.CurrentBlockTime.AddMinutes(10) // Maybe, get the interval from configuration
                },
                OriginProposer = proposedInfo.Proposer
            });
            
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
                contractProposingInput != null && contractProposingInput.Status == ContractProposingInputStatus.Proposed &&
                contractProposingInput.Proposer == Context.Sender, "Invalid contract proposing status.");
            contractProposingInput.Status = ContractProposingInputStatus.Approved;
            State.ContractProposingInputMap[input.ProposedContractInputHash] = contractProposingInput;
            State.ParliamentAuthContract.Release.Send(input.ProposalId);
            return new Empty();
        }
        
        public override Empty ReleaseCodeCheckedContract(ReleaseContractInput input)
        {
            var contractProposingInput = State.ContractProposingInputMap[input.ProposedContractInputHash];
            Assert(
                contractProposingInput != null && contractProposingInput.Status == ContractProposingInputStatus.PreCodeChecked &&
                contractProposingInput.Proposer == Context.Sender, "Invalid contract proposing status.");
            contractProposingInput.Status = ContractProposingInputStatus.CodeChecked;
            State.ContractProposingInputMap[input.ProposedContractInputHash] = contractProposingInput;
            State.ParliamentAuthContract.Release.Send(input.ProposalId);
            return new Empty();
        }

        
        public override Address DeploySmartContract(ContractDeploymentInput input)
        {
            RequireSenderAuthority();
            AssertDeploymentProposerAuthority(Context.Origin);

            var inputHash = CalculateHashFromInput(input);
            TryClearContractProposingInput(inputHash, out var contractProposingInput);

            var address =
                PrivateDeploySystemSmartContract(null, input.Category, input.Code.ToByteArray(), false,
                    DecideNormalContractAuthor(contractProposingInput?.Proposer ?? Context.Sender));
            return address;
        }

        public override Address UpdateSmartContract(ContractUpdateInput input)
        {
            var contractAddress = input.Address;
            var code = input.Code.ToByteArray();
            var info = State.ContractInfos[contractAddress];
            Assert(info != null, "Contract not found.");
            RequireSenderAuthority();
            var inputHash = CalculateHashFromInput(input);
            
            if (!TryClearContractProposingInput(inputHash, out _))
                Assert(Context.Sender == info.Author, "No permission.");

            var oldCodeHash = info.CodeHash;
            var newCodeHash = Hash.FromRawBytes(code);
            Assert(!oldCodeHash.Equals(newCodeHash), "Code is not changed.");

            info.CodeHash = newCodeHash;
            State.ContractInfos[contractAddress] = info;

            var reg = new SmartContractRegistration
            {
                Category = info.Category,
                Code = ByteString.CopyFrom(code),
                CodeHash = newCodeHash
            };

            State.SmartContractRegistrations[reg.CodeHash] = reg;

            Context.UpdateContract(contractAddress, reg, null);

            Context.Fire(new CodeUpdated()
            {
                Address = contractAddress,
                OldCodeHash = oldCodeHash,
                NewCodeHash = newCodeHash
            });

            Context.LogDebug(() => "BasicContractZero - update success: " + contractAddress.GetFormatted());
            return contractAddress;
        }
        
        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Contract zero already initialized.");
            Assert(Context.Sender == Context.Self, "Unable to initialize.");
            State.ContractDeploymentAuthorityRequired.Value = input.ContractDeploymentAuthorityRequired;
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty ChangeGenesisOwner(Address newOwnerAddress)
        {
            if (State.GenesisOwner.Value == null)
                InitializeGenesisOwner(newOwnerAddress);
            else
            {
                AssertSenderAddressWith(State.GenesisOwner.Value);
                RequireParliamentAuthAddressSet();
                var organizationExist = CheckOrganizationExist(newOwnerAddress);
                Assert(organizationExist, "Invalid genesis owner address.");
                State.GenesisOwner.Value = newOwnerAddress;
            }

            return new Empty();
        }

        public override Empty SetContractProposerRequiredState(BoolValue input)
        {
            Assert(!State.Initialized.Value, "Genesis contract already initialized.");
            var address = GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
            Assert(Context.Sender == address, "Unauthorized to set genesis contract state.");
            State.ContractProposerAuthorityRequired.Value = input.Value;
            return new Empty();
        }

        #endregion Actions
    }
}
﻿using System.Linq;
using Acs1;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        [View]
        public override TokenInfo GetTokenInfo(GetTokenInfoInput input)
        {
            return State.TokenInfos[input.Symbol];
        }

        public override TokenInfo GetNativeTokenInfo(Empty input)
        {
            return State.TokenInfos[State.NativeTokenSymbol.Value];
        }

        public override TokenInfoList GetResourceTokenInfo(Empty input)
        {
            var tokenInfoList = new TokenInfoList();
            foreach (var symbol in Context.Variables.SymbolListToPayTxFee.Where(symbol =>
                State.TokenInfos[symbol] != null))
            {
                tokenInfoList.Value.Add(State.TokenInfos[symbol]);
            }

            foreach (var symbol in Context.Variables.SymbolListToPayRental.Where(symbol =>
                State.TokenInfos[symbol] != null))
            {
                tokenInfoList.Value.Add(State.TokenInfos[symbol]);
            }

            return tokenInfoList;
        }

        [View]
        public override GetBalanceOutput GetBalance(GetBalanceInput input)
        {
            return new GetBalanceOutput()
            {
                Symbol = input.Symbol,
                Owner = input.Owner,
                Balance = GetBalance(input.Owner, input.Symbol)
            };
        }

        [View]
        public override GetAllowanceOutput GetAllowance(GetAllowanceInput input)
        {
            return new GetAllowanceOutput()
            {
                Symbol = input.Symbol,
                Owner = input.Owner,
                Spender = input.Spender,
                Allowance = State.Allowances[input.Owner][input.Spender][input.Symbol]
            };
        }

        public override BoolValue IsInWhiteList(IsInWhiteListInput input)
        {
            return new BoolValue {Value = State.LockWhiteLists[input.Symbol][input.Address]};
        }

        public override ProfitReceivingInformation GetProfitReceivingInformation(Address input)
        {
            return State.ProfitReceivingInfos[input] ?? new ProfitReceivingInformation();
        }

        public override GetLockedAmountOutput GetLockedAmount(GetLockedAmountInput input)
        {
            var virtualAddress = GetVirtualAddressForLocking(new GetVirtualAddressForLockingInput
            {
                Address = input.Address,
                LockId = input.LockId
            });
            return new GetLockedAmountOutput
            {
                Symbol = input.Symbol,
                Address = input.Address,
                LockId = input.LockId,
                Amount = GetBalance(virtualAddress, input.Symbol)
            };
        }

        public override Address GetVirtualAddressForLocking(GetVirtualAddressForLockingInput input)
        {
            var fromVirtualAddress = Hash.FromRawBytes(Context.Sender.Value.Concat(input.Address.Value)
                .Concat(input.LockId.Value).ToArray());
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(fromVirtualAddress);
            return virtualAddress;
        }

        public override Address GetCrossChainTransferTokenContractAddress(
            GetCrossChainTransferTokenContractAddressInput input)
        {
            return State.CrossChainTransferWhiteList[input.ChainId];
        }

        public override StringValue GetPrimaryTokenSymbol(Empty input)
        {
            if (string.IsNullOrWhiteSpace(_primaryTokenSymbol) && State.ChainPrimaryTokenSymbol.Value != null)
            {
                _primaryTokenSymbol = State.ChainPrimaryTokenSymbol.Value;
            }

            return new StringValue
            {
                Value = _primaryTokenSymbol ?? string.Empty
            };
        }

        public override CalculateFeeCoefficients GetCalculateFeeCoefficientsForContract(SInt32Value input)
        {
            if (input.Value == (int) FeeTypeEnum.Tx)
                return null;
            var targetTokenCoefficient =
                State.AllCalculateFeeCoefficients.Value.Value.FirstOrDefault(x =>
                    x.FeeTokenType == input.Value);
            return targetTokenCoefficient;
        }

        public override CalculateFeeCoefficients GetCalculateFeeCoefficientsForSender(Empty input)
        {
            var targetTokenCoefficient =
                State.AllCalculateFeeCoefficients.Value.Value.First(x =>
                    x.FeeTokenType == (int)FeeTypeEnum.Tx);
            return targetTokenCoefficient;
        }

        public override OwningRental GetOwningRental(Empty input)
        {
            var owingRental = new OwningRental();
            foreach (var symbol in Context.Variables.SymbolListToPayRental)
            {
                owingRental.ResourceAmount[symbol] = State.OwningRental[symbol];
            }

            return owingRental;
        }

        public override OwningRentalUnitValue GetOwningRentalUnitValue(Empty input)
        {
            var rentalResourceUnitValue = new OwningRentalUnitValue();
            foreach (var symbol in Context.Variables.SymbolListToPayRental)
            {
                rentalResourceUnitValue.ResourceUnitValue[symbol] = State.Rental[symbol];
            }

            return rentalResourceUnitValue;
        }

        public override ResourceUsage GetResourceUsage(Empty input)
        {
            var usage = new ResourceUsage();
            foreach (var symbol in Context.Variables.SymbolListToPayRental)
            {
                usage.Value.Add(symbol, State.ResourceAmount[symbol]);
            }

            return usage;
        }

        public override SymbolListToPayTxSizeFee GetSymbolsToPayTxSizeFee(Empty input)
        {
            return State.SymbolListToPayTxSizeFee.Value;
        }
        
        public override UserFeeController GetUserFeeController(Empty input)
        {
            Assert(State.UserFeeController.Value != null,
                "controller does not initialize, call InitializeAuthorizedController first");
            return State.UserFeeController.Value;
        }
        
        public override DeveloperFeeController GetDeveloperFeeController(Empty input)
        {
            Assert(State.DeveloperFeeController.Value != null,
                "controller does not initialize, call InitializeAuthorizedController first");
            return State.DeveloperFeeController.Value;
        }
        
        public override ControllerCreateInfo GetSideChainRentalControllerCreateInfo(Empty input)
        {
            Assert(State.SideChainCreator.Value != null, "side chain creator dose not exist");
            var organization = GetControllerCreateInputForSideChainRental().OrganizationCreationInput;
            var controllerAddress = CalculateSideChainRentalController(organization);
            var controllerInfo = new ControllerCreateInfo
            {
                Controller = controllerAddress,
                OrganizationCreationInputBytes = organization.ToByteString()
            };
            return controllerInfo;
        }

        public override AuthorityInfo GetSymbolsToPayTXSizeFeeController(Empty input)
        {
            if(State.SymbolToPayTxFeeController.Value == null)
                return GetDefaultSymbolToPayTxFeeController();
            return State.SymbolToPayTxFeeController.Value;
        }
    }
}
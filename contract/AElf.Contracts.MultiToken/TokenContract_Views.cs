using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
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
            return new TokenInfoList
            {
                Value =
                {
                    Context.Variables.ResourceTokenSymbolNameList.Select(symbol =>
                        State.TokenInfos[symbol] ?? new TokenInfo())
                }
            };
        }

        [View]
        public override GetBalanceOutput GetBalance(GetBalanceInput input)
        {
            return new GetBalanceOutput()
            {
                Symbol = input.Symbol,
                Owner = input.Owner,
                Balance = State.Balances[input.Owner][input.Symbol]
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
                Amount = State.Balances[virtualAddress][input.Symbol]
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
            return new StringValue
            {
                Value = (State.ChainPrimaryTokenSymbol.Value ?? State.NativeTokenSymbol.Value) ?? string.Empty
            };
        }

        public override SInt64Value GetTransactionSizeFeeUnitPrice(Empty input)
        {
            return new SInt64Value {Value = State.TransactionFeeUnitPrice.Value};
        }
        public override CalculateFeeCoefficientsOfType GetCalculateFeeCoefficientOfContract(SInt32Value input)
        {
            TryToInitialParameters();
            return State.CalculateCoefficientOfContract[(FeeTypeEnum)input.Value];
        }
        public override CalculateFeeCoefficientsOfType GetCalculateFeeCoefficientOfSender(Empty input)
        {
            TryToInitialParameters();
            return State.CalculateCoefficientOfSender;
        }
        private CalculateFeeCoefficientsOfType GetCpuFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var cpuFeeParameter1 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
                CoefficientDic = { {"numerator", 1},{"denominator", 8},{"constantvalue", 1000}}
                
            };
            var cpuFeeParameter2 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 100,
                CoefficientDic = { {"numerator", 1},{"denominator", 4}}
            };
            var cpuFeeParameter3 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1},{"denominator", 4},{"power", 2},{"changespanbase", 5},{"weight", 250},
                    {"weightbase", 40}
                }
            };
            totalParameter.Coefficients.Add(cpuFeeParameter1);
            totalParameter.Coefficients.Add(cpuFeeParameter2);
            totalParameter.Coefficients.Add(cpuFeeParameter3);
            return totalParameter;
        }
        private CalculateFeeCoefficientsOfType GetStoFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var stoFeeParameter1 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator", 1},{"denominator", 4},{"constantvalue", 1000}}
                
            };
            var stoFeeParameter2 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1},{"denominator", 64},{"power", 2},{"changespanbase", 100},{"weight", 250},
                    {"weightbase", 500}
                }
            };
            totalParameter.Coefficients.Add(stoFeeParameter1);
            totalParameter.Coefficients.Add(stoFeeParameter2);
            return totalParameter;
        }
        private CalculateFeeCoefficientsOfType GetRamFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var ramFeeParameter1 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
                CoefficientDic = { {"numerator", 1},{"denominator", 8},{"constantvalue", 10000}}
                
            };
            var ramFeeParameter2 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 100,
                CoefficientDic = { {"numerator", 1},{"denominator", 4}}
            };
            var ramFeeParameter3 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1},{"denominator", 4},{"power", 2},{"changespanbase", 2},{"weight", 250},
                    {"weightbase", 40}
                }
            };
            totalParameter.Coefficients.Add(ramFeeParameter1);
            totalParameter.Coefficients.Add(ramFeeParameter2);
            totalParameter.Coefficients.Add(ramFeeParameter3);
            return totalParameter;
        }
        private CalculateFeeCoefficientsOfType GetNetFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var netFeeParameter1 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator", 1},{"denominator", 64},{"constantvalue", 10000}}
                
            };
            var netFeeParameter2 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1},{"denominator", 64},{"power", 2},{"changespanbase", 100},{"weight", 250},
                    {"weightbase", 500}
                }
            };
            totalParameter.Coefficients.Add(netFeeParameter1);
            totalParameter.Coefficients.Add(netFeeParameter2);
            return totalParameter;
        }
        private CalculateFeeCoefficientsOfType GetTxFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var txFeeParameter1 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator", 1},{"denominator", 800},{"constantvalue", 10000}}
            };
            var txFeeParameter2 = new CalculateFeeCoefficient
            {
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1},{"denominator", 800},{"power", 2},{"changespanbase", 100},{"weight", 1},
                    {"weightbase", 1}
                }
            };
            totalParameter.Coefficients.Add(txFeeParameter1);
            totalParameter.Coefficients.Add(txFeeParameter2);
            return totalParameter;
        }

        #region ForTests

        /*
        [View]
        
        public string GetTokenInfo2(string symbol)
        {
            return GetTokenInfo(new GetTokenInfoInput() {Symbol = symbol}).ToString();
        }

        [View]
        public string GetBalance2(string symbol, Address owner)
        {
            return GetBalance(
                new GetBalanceInput() {Symbol = symbol, Owner = owner})?.ToString();
        }

        [View]
        public string GetAllowance2(string symbol, Address owner, Address spender)
        {
            return GetAllowance(new GetAllowanceInput()
            {
                Owner = owner,
                Symbol = symbol,
                Spender = spender
            })?.ToString();
        }
        */

        #endregion
    }
}
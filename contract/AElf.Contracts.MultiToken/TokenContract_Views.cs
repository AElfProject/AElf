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

        public override AllCalculateFeeParameter GetCalculateFeeAllParameters(SInt32Value input)
        {
            IntialParameters();
            switch (input.Value)
            {
                case 0:
                    return State.CalculateCoefficient[(int) FeeTypeEnum.Tx];
                case 1:
                    return State.CalculateCoefficient[(int) FeeTypeEnum.Cpu];
                case 2:
                    return State.CalculateCoefficient[(int) FeeTypeEnum.Sto];
                case 3:
                    return State.CalculateCoefficient[(int) FeeTypeEnum.Ram];
                case 4:
                    return State.CalculateCoefficient[(int) FeeTypeEnum.Net];
                default:
                    return null;
            }
        }
        private AllCalculateFeeParameter GetCpuFeeParameter()
        {
            var totalParameter = new AllCalculateFeeParameter();
            var cpuFeeParameter1 = new CalculateFeeParameter
            {
                FunctionType = 2,
                PieceKey = 10,
                ParameterDic = { {"numerator","1"},{"denominator","8"},{"constantValue","10000"}}
                
            };
            var cpuFeeParameter2 = new CalculateFeeParameter
            {
                FunctionType = 2,
                PieceKey = 100,
                ParameterDic = { {"numerator","1"},{"denominator","4"}}
            };
            var cpuFeeParameter3 = new CalculateFeeParameter
            {
                FunctionType =3,
                PieceKey = int.MaxValue,
                ParameterDic =
                {
                    {"numerator","1"},{"denominator","4"},{"power","2"},{"changeSpanBase","4"},{"weight","250"},
                    {"weightBase", "40"}
                }
            };
            totalParameter.AllParameter.Add(cpuFeeParameter1);
            totalParameter.AllParameter.Add(cpuFeeParameter2);
            totalParameter.AllParameter.Add(cpuFeeParameter3);
            return totalParameter;
        }
        private AllCalculateFeeParameter GetStoFeeParameter()
        {
            var totalParameter = new AllCalculateFeeParameter();
            var stoFeeParameter1 = new CalculateFeeParameter
            {
                FunctionType = 2,
                PieceKey = 1000000,
                ParameterDic = { {"numerator","1"},{"denominator","4"},{"constantValue", "10000"}}
                
            };
            var stoFeeParameter2 = new CalculateFeeParameter
            {
                FunctionType = 3,
                PieceKey = int.MaxValue,
                ParameterDic =
                {
                    {"numerator","1"},{"denominator","64"},{"power","2"},{"changeSpanBase","100"},{"weight","250"},
                    {"weightBase", "500"}
                }
            };
            totalParameter.AllParameter.Add(stoFeeParameter1);
            totalParameter.AllParameter.Add(stoFeeParameter2);
            return totalParameter;
        }
        private AllCalculateFeeParameter GetRamFeeParameter()
        {
            var totalParameter = new AllCalculateFeeParameter();
            var ramFeeParameter1 = new CalculateFeeParameter
            {
                FunctionType = 2,
                PieceKey = 10,
                ParameterDic = { {"numerator","1"},{"denominator","8"},{"constantValue","10000"}}
                
            };
            var ramFeeParameter2 = new CalculateFeeParameter
            {
                FunctionType = 2,
                PieceKey = 100,
                ParameterDic = { {"numerator","1"},{"denominator","4"}}
            };
            var ramFeeParameter3 = new CalculateFeeParameter
            {
                FunctionType = 3,
                PieceKey = int.MaxValue,
                ParameterDic =
                {
                    {"numerator","1"},{"denominator","4"},{"power","2"},{"changeSpanBase","2"},{"weight","250"},
                    {"weightBase", "40"}
                }
            };
            totalParameter.AllParameter.Add(ramFeeParameter1);
            totalParameter.AllParameter.Add(ramFeeParameter2);
            totalParameter.AllParameter.Add(ramFeeParameter3);
            return totalParameter;
        }
        private AllCalculateFeeParameter GetNetFeeParameter()
        {
            var totalParameter = new AllCalculateFeeParameter();
            var netFeeParameter1 = new CalculateFeeParameter
            {
                FunctionType = 2,
                PieceKey = 1000000,
                ParameterDic = { {"numerator","1"},{"denominator","64"},{"constantValue","10000"}}
                
            };
            var netFeeParameter2 = new CalculateFeeParameter
            {
                FunctionType = 3,
                PieceKey = int.MaxValue,
                ParameterDic =
                {
                    {"numerator","1"},{"denominator","64"},{"power","2"},{"changeSpanBase","100"},{"weight","250"},
                    {"weightBase", "500"}
                }
            };
            totalParameter.AllParameter.Add(netFeeParameter1);
            totalParameter.AllParameter.Add(netFeeParameter2);
            return totalParameter;
        }
        private AllCalculateFeeParameter GetTxFeeParameter()
        {
            var totalParameter = new AllCalculateFeeParameter();
            var txFeeParameter1 = new CalculateFeeParameter
            {
                FunctionType = 2,
                PieceKey = 1000000,
                ParameterDic = { {"numerator","1"},{"denominator",(16.Mul(50)).ToString()},{"constantValue","10000"}}
            };
            var txFeeParameter2 = new CalculateFeeParameter
            {
                FunctionType = 3,
                PieceKey = int.MaxValue,
                ParameterDic =
                {
                    {"numerator","1"},{"denominator",(16.Mul(50)).ToString()},{"power","2"},{"changeSpanBase","100"},{"weight","1"},
                    {"weightBase", "1"}
                }
            };
            totalParameter.AllParameter.Add(txFeeParameter1);
            totalParameter.AllParameter.Add(txFeeParameter2);
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
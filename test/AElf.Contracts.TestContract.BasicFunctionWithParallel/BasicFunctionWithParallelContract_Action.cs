using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicFunctionWithParallel
{
    /// <summary>
    /// Action methods
    /// </summary>
    public partial class BasicFunctionWithParallelContract : BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractBase
    {
        public override Empty InitialBasicFunctionWithParallelContract(InitialBasicFunctionWithParallelContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            Assert(input.MinValue >0 && input.MaxValue >0 && input.MaxValue >= input.MinValue, "Invalid min/max value input setting.");
            
            State.Initialized.Value = true;
            State.ContractName.Value = input.ContractName;
            State.ContractManager.Value = input.Manager;
            State.MinBet.Value = input.MinValue;
            State.MaxBet.Value = input.MaxValue;
            State.MortgageBalance.Value = input.MortgageValue;

            return new Empty();
        }

        public override Empty UpdateBetLimit(BetLimitInput input)
        {
            Assert(Context.Sender == State.ContractManager.Value, "Only manager can perform this action."); 
            Assert(input.MinValue >0 && input.MaxValue >0 && input.MaxValue >= input.MinValue, "Invalid min/max value input setting.");
            
            State.MinBet.Value = input.MinValue;
            State.MaxBet.Value = input.MaxValue;

            return new Empty();
        }

        public override Empty UserPlayBet(BetInput input)
        {
            Assert(input.Int64Value >= State.MinBet.Value && input.Int64Value <=State.MaxBet.Value, $"Input balance not in boundary({State.MinBet.Value}, {State.MaxBet.Value}).");
            //Assert(input.Int64Value > State.WinerHistory[Context.Sender], "Should bet bigger than your reward money.");
            State.TotalBetBalance.Value = State.TotalBetBalance.Value.Add(input.Int64Value);
            
            var result = WinOrLose(input.Int64Value);

            if (result == 0)
            {
                State.LoserHistory[Context.Sender] = State.LoserHistory[Context.Sender].Add(input.Int64Value);
            }
            else
            {
                State.RewardBalance.Value = State.RewardBalance.Value.Add(result);
                State.WinnerHistory[Context.Sender] = State.WinnerHistory[Context.Sender].Add(result);
            }
            
            return new Empty();
        }

        public override Empty LockToken(LockTokenInput input)
        {
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            
            State.TokenContract.Lock.Send(new LockInput
            {
                Symbol = input.Symbol,
                Address = input.Address,
                Amount = input.Amount,
                LockId = input.LockId,
                Usage = input.Usage
            });
            
            return new Empty();
        }

        public override Empty UnlockToken(UnlockTokenInput input)
        {
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Symbol = input.Symbol,
                Address = input.Address,
                Amount = input.Amount,
                LockId = input.LockId,
                Usage = input.Usage
            });
            return new Empty();
        }

        public override Empty ValidateOrigin(Address address)
        {
            Assert(address == Context.Origin, "Validation failed, origin is not expected.");
            return new Empty();
        }

        public override Empty IncreaseWinMoney(IncreaseWinMoneyInput input)
        {
            State.WinnerHistory[input.First] = State.WinnerHistory[input.First] + 1;
            State.WinnerHistory[input.Second] = State.WinnerHistory[input.Second] + 1;
            return new Empty();
        }

        public override Empty SetValue(SetValueInput input)
        {
            State.LongValueMap[input.Key] = input.Int64Value;
            State.StringValueMap[input.Key] = input.StringValue;
            State.MessageValueMap[input.Key] = input.MessageValue;
            return new Empty();
        }

        public override Empty IncreaseValue(IncreaseValueInput input)
        {
            var longValue = State.LongValueMap[input.Key];
            longValue++;
            State.LongValueMap[input.Key] = longValue;
            State.StringValueMap[input.Key] = longValue.ToString();
            var messageValue = State.MessageValueMap[input.Key] ?? new MessageValue();
            messageValue.Int64Value = longValue;
            messageValue.StringValue = State.StringValueMap[input.Key];
            State.MessageValueMap[input.Key] = messageValue;
            return new Empty();
        }

        public override Empty IncreaseValueParallel(IncreaseValueInput input)
        {
            return IncreaseValue(input);
        }
        
        public override Empty IncreaseValueWithInline(IncreaseValueInput input)
        {
            IncreaseValue(input);
            Context.SendInline(Context.Self, nameof(IncreaseValue), input);
            return new Empty();
        }

        public override Empty IncreaseValueWithPrePlugin(IncreaseValueInput input)
        {
            return IncreaseValue(input);
        }
        
        public override Empty IncreaseValueWithPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValue(input);
        }

        public override Empty IncreaseValueWithInlineAndPrePlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithInline(input);
        }
        
        public override Empty IncreaseValueWithInlineAndPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithInline(input);
        }

        public override Empty IncreaseValueWithPlugin(IncreaseValueInput input)
        {
            return IncreaseValue(input);
        }

        public override Empty IncreaseValueWithInlineAndPlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithInline(input);
        }
        
        public override Empty IncreaseValueParallelWithInlineAndPlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithInline(input);
        }

        public override Empty IncreaseValueFailed(IncreaseValueInput input)
        {
            Assert(false);
            return new Empty();
        }

        public override Empty IncreaseValueParallelFailed(IncreaseValueInput input)
        {
            return IncreaseValueFailed(input);
        }

        public override Empty IncreaseValueFailedWithInline(IncreaseValueInput input)
        {
            Context.SendInline(Context.Self, nameof(IncreaseValue), input);
            return IncreaseValueFailed(input);
        }

        public override Empty IncreaseValueFailedWithPlugin(IncreaseValueInput input)
        {
            return IncreaseValueFailed(input);
        }

        public override Empty IncreaseValueFailedWithPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValueFailed(input);
        }

        public override Empty IncreaseValueFailedWithPrePlugin(IncreaseValueInput input)
        {
            return IncreaseValueFailed(input);
        }

        public override Empty IncreaseValueFailedWithInlineAndPlugin(IncreaseValueInput input)
        {
            return IncreaseValueFailedWithInline(input);
        }
        
        public override Empty IncreaseValueWithFailedInline(IncreaseValueInput input)
        {
            IncreaseValue(input);
            Context.SendInline(Context.Self, nameof(IncreaseValueFailed), input);
            return new Empty();
        }

        public override Empty IncreaseValueWithFailedInlineAndPlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithFailedInline(input);
        }

        public override Empty IncreaseValueFailedParallelWithInlineAndPlugin(IncreaseValueInput input)
        {
            return IncreaseValueFailedWithInline(input);
        }

        public override Empty IncreaseValueFailedWithInlineAndPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValueFailedWithInline(input);
        }

        public override Empty IncreaseValueFailedWithInlineAndPrePlugin(IncreaseValueInput input)
        {
            return IncreaseValueFailedWithInline(input);
        }
        
        public override Empty IncreaseValueParallelWithFailedInlineAndPlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithFailedInline(input);
        }

        public override Empty IncreaseValueWithFailedInlineAndPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithFailedInline(input);
        }

        public override Empty IncreaseValueWithFailedInlineAndPrePlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithFailedInline(input);
        }

        public override Empty IncreaseValueWithFailedPrePlugin(IncreaseValueInput input)
        {
            return IncreaseValue(input);
        }

        public override Empty IncreaseValueWithInlineAndFailedPrePlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithInline(input);
        }

        public override Empty IncreaseValueWithFailedPrePluginAndPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValue(input);
        }

        public override Empty IncreaseValueWithInlineAndFailedPrePluginAndPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithInline(input);
        }

        public override Empty IncreaseValueWithFailedPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValue(input);
        }

        public override Empty IncreaseValueWithInlineAndFailedPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithInline(input);
        }

        public override Empty IncreaseValueWithPrePluginAndFailedPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValue(input);
        }

        public override Empty IncreaseValueWithInlineAndPrePluginAndFailedPostPlugin(IncreaseValueInput input)
        {
            return IncreaseValueWithInline(input);
        }

        public override Empty RemoveValue(RemoveValueInput input)
        {
            State.LongValueMap.Remove(input.Key);
            State.StringValueMap.Remove(input.Key);
            State.MessageValueMap.Remove(input.Key);
            return new Empty();
        }

        public override Empty RemoveValueFromInlineWithPlugin(RemoveValueInput input)
        {
            IncreaseValue(new IncreaseValueInput
            {
                Key = input.Key
            });
            Context.SendInline(Context.Self, nameof(RemoveValue), input);
            return new Empty();
        }

        public override Empty RemoveValueFromPrePlugin(RemoveValueInput input)
        {
            var increaseValueInput = new IncreaseValueInput
            {
                Key = input.Key
            };
            IncreaseValue(increaseValueInput);
            Context.SendInline(Context.Self, nameof(IncreaseValue), increaseValueInput);
            return new Empty();
        }
        
        public override Empty RemoveValueFromPostPlugin(RemoveValueInput input)
        {
            var increaseValueInput = new IncreaseValueInput
            {
                Key = input.Key
            };
            IncreaseValue(increaseValueInput);
            Context.SendInline(Context.Self, nameof(IncreaseValue), increaseValueInput);
            return new Empty();
        }

        public override Empty RemoveValueParallelFromPostPlugin(RemoveValueInput input)
        {
            var increaseValueInput = new IncreaseValueInput
            {
                Key = input.Key
            };
            IncreaseValue(increaseValueInput);
            Context.SendInline(Context.Self, nameof(IncreaseValue), increaseValueInput);
            return new Empty();
        }

        public override Empty RemoveValueWithPlugin(RemoveValueInput input)
        {
            RemoveValue(input);
            var increaseValueInput = new IncreaseValueInput
            {
                Key = input.Key
            };
            Context.SendInline(Context.Self, nameof(IncreaseValue), increaseValueInput);
            return new Empty();
        }

        public override Empty RemoveAfterSetValue(RemoveAfterSetValueInput input)
        {
            State.LongValueMap[input.Key] = input.Int64Value;
            State.StringValueMap[input.Key] = input.StringValue;
            State.MessageValueMap[input.Key] = input.MessageValue;
            
            State.LongValueMap.Remove(input.Key);
            State.StringValueMap.Remove(input.Key);
            State.MessageValueMap.Remove(input.Key);
            
            return new Empty();
        }

        public override Empty SetAfterRemoveValue(SetAfterRemoveValueInput input)
        {
            State.LongValueMap.Remove(input.Key);
            State.StringValueMap.Remove(input.Key);
            State.MessageValueMap.Remove(input.Key);
            
            State.LongValueMap[input.Key] = input.Int64Value;
            State.StringValueMap[input.Key] = input.StringValue;
            State.MessageValueMap[input.Key] = input.MessageValue;
            
            return new Empty();
        }

        public override Empty RemoveValueParallel(RemoveValueInput input)
        {
            return RemoveValue(input);
        }

        public override Empty ComplexChangeWithDeleteValue1(ComplexChangeInput input)
        {
            State.LongValueMap[input.Key] = input.Int64Value;
            State.StringValueMap[input.Key] = input.StringValue;
            
            State.MessageValueMap.Remove(input.Key);
            
            return new Empty();
        }

        public override Empty ComplexChangeWithDeleteValue2(ComplexChangeInput input)
        {
            State.LongValueMap.Remove(input.Key);
            
            State.StringValueMap[input.Key] = input.StringValue;
            State.MessageValueMap[input.Key] = input.MessageValue;
            
            return new Empty();
        }

        public override Empty ComplexChangeWithDeleteValue3(ComplexChangeInput input)
        {
            State.LongValueMap[input.Key] = input.Int64Value;
            
            State.StringValueMap.Remove(input.Key);
            State.MessageValueMap.Remove(input.Key);
            
            return new Empty();
        }

        private long WinOrLose(long betAmount)
        {
            var data = State.TotalBetBalance.Value.Sub(State.RewardBalance.Value);
            if(data < 0)
                data = data.Mul(-1);
                
            if (data % 100 == 1)
                return betAmount.Mul(1000);
            if (data % 50 == 5)
                return betAmount.Mul(50);
            return 0;
        }
    }
}
using Acs8;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract
{
    public class Contract : ContractContainer.ContractBase
    {
        public override Empty BuyResourceToken(BuyResourceTokenInput input)
        {
            if (State.TokenConverterContract.Value == null)
            {
                State.TokenConverterContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);
            }

            if (input.Amount > 0)
            {
                State.TokenConverterContract.Buy.Send(new BuyInput
                {
                    Symbol = input.Symbol,
                    Amount = input.Amount,
                    PayLimit = input.PayLimit
                });
            }

            return new Empty();
        }

        /// <summary>
        /// Time consuming.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty CpuConsumingMethod(Empty input)
        {
            var sum = 0L;
            for (var i = 0; i < 999_99; i++)
            {
                sum = sum.Add(i);
            }

            State.Map[sum.ToString()] = sum.ToString();
            return new Empty();
        }

        /// <summary>
        /// Large writes count.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty StoConsumingMethod(Empty input)
        {
            for (var i = 0; i < 999; i++)
            {
                State.Map[i.ToString()] = i.ToString();
            }
            
            return new Empty();
        }

        /// <summary>
        /// Large parameter.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty NetConsumingMethod(NetConsumingMethodInput input)
        {
            return new Empty();
        }

        public override Empty FewConsumingMethod(Empty input)
        {
            return new Empty();
        }
    }
}
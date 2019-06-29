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

            State.TokenConverterContract.Buy.Send(new BuyInput
            {
                Symbol = input.Symbol,
                Amount = input.Amount,
                PayLimit = input.PayLimit
            });

            return new Empty();
        }

        public override Empty SetResourceTokenBuyingPreferences(ResourceTokenBuyingPreferences input)
        {
            if (State.Acs0Contract.Value == null)
            {
                State.Acs0Contract.Value = Context.GetZeroSmartContractAddress();
            }

            Assert(State.Acs0Contract.GetContractOwner.Call(Context.Self) == Context.Sender,
                "Only owner can set resource token buying preferences.");
            State.ResourceTokenBuyingPreferences.Value = input;
            return new Empty();
        }

        /// <summary>
        /// Time consuming.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty CpuConsumingMethod(Empty input)
        {
            var sum = 0;
            for (var i = 0; i < int.MaxValue.Div(1000); i++)
            {
                sum = sum.Add(i);
            }

            return new Empty();
        }

        /// <summary>
        /// Large writes count.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty StoConsumingMethod(Empty input)
        {
            for (var i = 0; i < 100_000; i++)
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
    }
}
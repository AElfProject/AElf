using Acs8;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests.TestContract
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
            for (var i = 0; i < 99; i++)
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
            for (var i = 0; i < 99; i++)
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

        public override Empty SendTransferFromAsInlineTx(SInt64Value input)
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = input.Value,
                Symbol = "ELF"
            });

            return new Empty();
        }
    }
}
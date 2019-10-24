using Acs2;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.TestContract.BasicFunctionWithParallel
{
    public partial class BasicFunctionWithParallelContract
    {
        public override ResourceInfo GetResourceInfo(Transaction txn)
        {
            switch (txn.MethodName)
            {
                case nameof(QueryTwoUserWinMoney):
                {
                    var args = QueryTwoUserWinMoneyInput.Parser.ParseFrom(txn.Params);
                    return new ResourceInfo
                    {
                        Paths =
                        {
                            GetPath(nameof(BasicFunctionWithParallelContractState.WinerHistory),args.Second.ToString())
                        }
                    };
                }
                case nameof(SetValue):
                {
                    var args = SetValueInput.Parser.ParseFrom(txn.Params);
                    return new ResourceInfo
                    {
                        Paths =
                        {
                            GetPath(nameof(BasicFunctionWithParallelContractState.LongValueMap),args.Key),
                            GetPath(nameof(BasicFunctionWithParallelContractState.StringValueMap),args.Key),
                            GetPath(nameof(BasicFunctionWithParallelContractState.MessageValueMap),args.Key)
                        }
                    };
                }
                case nameof(RemoveValueParallel):
                case nameof(RemoveValueParallelFromPostPlugin):
                {
                    var args = RemoveValueInput.Parser.ParseFrom(txn.Params);
                    return new ResourceInfo
                    {
                        Paths =
                        {
                            GetPath(nameof(BasicFunctionWithParallelContractState.LongValueMap),args.Key),
                            GetPath(nameof(BasicFunctionWithParallelContractState.StringValueMap),args.Key),
                            GetPath(nameof(BasicFunctionWithParallelContractState.MessageValueMap),args.Key)
                        }
                    };
                }
                
                case nameof(IncreaseValueParallel):
                case nameof(IncreaseValueParallelWithInlineAndPlugin):
                {
                    var args = IncreaseValueInput.Parser.ParseFrom(txn.Params);
                    return new ResourceInfo
                    {
                        Paths =
                        {
                            GetPath(nameof(BasicFunctionWithParallelContractState.LongValueMap),args.Key),
                            GetPath(nameof(BasicFunctionWithParallelContractState.StringValueMap),args.Key),
                            GetPath(nameof(BasicFunctionWithParallelContractState.MessageValueMap),args.Key)
                        }
                    };
                }
                default:
                    throw new AssertionException($"invalid method: {txn.MethodName}");
            }
        }

        private ScopedStatePath GetPath(params string[] parts)
        {
            return new ScopedStatePath
            {
                Address = Context.Self,
                Path = new StatePath
                {
                    Parts =
                    {
                        parts
                    }
                }
            };
        }
    }
}
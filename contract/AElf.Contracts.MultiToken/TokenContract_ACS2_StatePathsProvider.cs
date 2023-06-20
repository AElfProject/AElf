using System;
using System.Data.SqlTypes;
using System.Linq;
using AElf.Standards.ACS2;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    public override ResourceInfo GetResourceInfo(Transaction txn)
    {
        switch (txn.MethodName)
        {
            case nameof(Transfer):
            {
                var args = TransferInput.Parser.ParseFrom(txn.Params);
                var resourceInfo = new ResourceInfo
                {
                    WritePaths =
                    {
                        GetPath(nameof(TokenContractState.Balances), txn.From.ToString(), args.Symbol),
                        GetPath(nameof(TokenContractState.Balances), args.To.ToString(), args.Symbol)
                    },
                    ReadPaths =
                    {
                        GetPath(nameof(TokenContractState.TokenInfos), args.Symbol),
                        GetPath(nameof(TokenContractState.ChainPrimaryTokenSymbol)),
                        GetPath(nameof(TokenContractState.TransactionFeeFreeAllowancesSymbolList))
                    }
                };
                AddPathForTransactionFee(resourceInfo, txn.From.ToString());
                AddPathForDelegatees(resourceInfo, txn.From, args.Symbol);
                AddPathForTransactionFeeFreeAllowance(resourceInfo, txn.From);
                
                return resourceInfo;
            }

            case nameof(TransferFrom):
            {
                var args = TransferFromInput.Parser.ParseFrom(txn.Params);
                var resourceInfo = new ResourceInfo
                {
                    WritePaths =
                    {
                        GetPath(nameof(TokenContractState.Allowances), args.From.ToString(), txn.From.ToString(),
                            args.Symbol),
                        GetPath(nameof(TokenContractState.Balances), args.From.ToString(), args.Symbol),
                        GetPath(nameof(TokenContractState.Balances), args.To.ToString(), args.Symbol),
                        GetPath(nameof(TokenContractState.LockWhiteLists), args.Symbol, txn.From.ToString())
                    },
                    ReadPaths =
                    {
                        GetPath(nameof(TokenContractState.TokenInfos), args.Symbol),
                        GetPath(nameof(TokenContractState.ChainPrimaryTokenSymbol)),
                        GetPath(nameof(TokenContractState.TransactionFeeFreeAllowancesSymbolList))
                    }
                };
                AddPathForTransactionFee(resourceInfo, txn.From.ToString());
                AddPathForDelegatees(resourceInfo, txn.From, args.Symbol);
                AddPathForTransactionFeeFreeAllowance(resourceInfo, txn.From);
                
                return resourceInfo;
            }

            default:
                return new ResourceInfo { NonParallelizable = true };
        }
    }

    private void AddPathForTransactionFee(ResourceInfo resourceInfo, string from)
    {
        var symbols = GetMethodFeeSymbols();
        var primaryTokenSymbol = GetPrimaryTokenSymbol(new Empty()).Value;
        if (_primaryTokenSymbol != string.Empty && !symbols.Contains(primaryTokenSymbol))
            symbols.Add(primaryTokenSymbol);
        var paths = symbols.Select(symbol => GetPath(nameof(TokenContractState.Balances), from, symbol));
        foreach (var path in paths)
        {
            if (resourceInfo.WritePaths.Contains(path)) continue;
            resourceInfo.WritePaths.Add(path);
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
    
    private void AddPathForDelegatees(ResourceInfo resourceInfo, Address from, string symbol)
    {
        var allDelegatees = State.TransactionFeeDelegateesMap[from];
        if (allDelegatees != null)
        {
            foreach (var delegations in allDelegatees.Delegatees.Keys)
            {
                if (delegations == null) return;
                var add = Address.FromBase58(delegations).ToString();
                AddPathForTransactionFee(resourceInfo, add);
                AddPathForTransactionFeeFreeAllowance(resourceInfo, Address.FromBase58(delegations));
                resourceInfo.WritePaths.Add(GetPath(nameof(TokenContractState.TransactionFeeFreeAllowances), add, symbol));
                resourceInfo.WritePaths.Add(GetPath(nameof(TokenContractState.TransactionFeeFreeAllowancesLastRefreshTimes), add, symbol));
            }
        }
    }

    private void AddPathForTransactionFeeFreeAllowance(ResourceInfo resourceInfo, Address from)
    {
        var symbols = State.TransactionFeeFreeAllowancesSymbolList.Value?.Symbols;
        if (symbols != null)
        {
            foreach (var symbol in symbols)
            {
                resourceInfo.WritePaths.Add(GetPath(nameof(TokenContractState.TransactionFeeFreeAllowances), from.ToBase58(), symbol));
                resourceInfo.WritePaths.Add(GetPath(nameof(TokenContractState.TransactionFeeFreeAllowancesLastRefreshTimes), from.ToBase58(), symbol));

                var path = GetPath(nameof(TokenContractState.TransactionFeeFreeAllowancesConfigMap), symbol);
                if (!resourceInfo.ReadPaths.Contains(path))
                {
                    resourceInfo.ReadPaths.Add(path);
                }
            }
        }
    }
}
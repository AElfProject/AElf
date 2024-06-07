using System.Collections.Generic;
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

                AddPathForTransactionFee(resourceInfo, txn.From.ToString(), txn.MethodName);
                AddPathForDelegatees(resourceInfo, txn.From, txn.To, txn.MethodName);
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
                AddPathForAllowance(resourceInfo, args.From.ToString(), txn.From.ToString(), args.Symbol);
                AddPathForTransactionFee(resourceInfo, txn.From.ToString(), txn.MethodName);
                AddPathForDelegatees(resourceInfo, txn.From, txn.To, txn.MethodName);
                AddPathForTransactionFeeFreeAllowance(resourceInfo, txn.From);

                return resourceInfo;
            }

            default:
                return new ResourceInfo { NonParallelizable = true };
        }
    }

    private void AddPathForAllowance(ResourceInfo resourceInfo, string from, string spender, string symbol)
    {
        resourceInfo.WritePaths.Add(GetPath(nameof(TokenContractState.Allowances), from, spender, symbol));
        resourceInfo.WritePaths.Add(GetPath(nameof(TokenContractState.Allowances), from, spender,
            GetAllSymbolIdentifier()));
        var symbolType = GetSymbolType(symbol);
        if (symbolType == SymbolType.Nft || symbolType == SymbolType.NftCollection)
        {
            resourceInfo.WritePaths.Add(GetPath(nameof(TokenContractState.Allowances), from, spender,
                GetNftCollectionAllSymbolIdentifier(symbol)));
        }
    }

    private void AddPathForTransactionFee(ResourceInfo resourceInfo, string from, string methodName)
    {
        var symbols = GetTransactionFeeSymbols(methodName);
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

    private void AddPathForDelegatees(ResourceInfo resourceInfo, Address from, Address to, string methodName)
    {
        var delegateeList = new List<string>();
        //get and add first-level delegatee list
        delegateeList.AddRange(GetDelegateeList(from, to, methodName));
        if (delegateeList.Count <= 0) return;
        var secondDelegateeList = new List<string>();
        //get and add second-level delegatee list
        foreach (var delegateeAddress in delegateeList.Select(a => Address.FromBase58(a)))
        {
            //delegatee of the first-level delegate is delegator of the second-level delegate
            secondDelegateeList.AddRange(GetDelegateeList(delegateeAddress, to, methodName));
        }
        delegateeList.AddRange(secondDelegateeList);
        foreach (var delegatee in delegateeList.Distinct())
        {
            AddPathForTransactionFee(resourceInfo, delegatee, methodName);
            AddPathForTransactionFeeFreeAllowance(resourceInfo, Address.FromBase58(delegatee));
        }
    }

    private List<string> GetDelegateeList(Address delegator, Address to, string methodName)
    {
        var delegateeList = new List<string>();
        var allDelegatees = State.TransactionFeeDelegateInfoMap[delegator][to][methodName] 
                            ?? State.TransactionFeeDelegateesMap[delegator];
            
        if (allDelegatees != null)
        {
            delegateeList.AddRange(allDelegatees.Delegatees.Keys.ToList());
        } 

        return delegateeList;
    }

    private void AddPathForTransactionFeeFreeAllowance(ResourceInfo resourceInfo, Address from)
    {
        var symbols = State.TransactionFeeFreeAllowancesSymbolList.Value?.Symbols;
        if (symbols != null)
        {
            foreach (var symbol in symbols)
            {
                resourceInfo.WritePaths.Add(GetPath(nameof(TokenContractState.TransactionFeeFreeAllowances),
                    from.ToBase58(), symbol));
                resourceInfo.WritePaths.Add(GetPath(
                    nameof(TokenContractState.TransactionFeeFreeAllowancesLastRefreshTimes), from.ToBase58(), symbol));

                var path = GetPath(nameof(TokenContractState.TransactionFeeFreeAllowancesConfigMap), symbol);
                if (!resourceInfo.ReadPaths.Contains(path))
                {
                    resourceInfo.ReadPaths.Add(path);
                }
            }
        }
    }
}
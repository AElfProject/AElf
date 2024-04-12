using System.Collections.Generic;
using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    public override SetTransactionFeeDelegationsOutput SetTransactionFeeDelegations(
        SetTransactionFeeDelegationsInput input)
    {
        AssertValidInputAddress(input.DelegatorAddress);
        Assert(input.Delegations != null, "Delegations cannot be null!");

        // get all delegatees, init it if null.
        var allDelegatees = State.TransactionFeeDelegateesMap[input.DelegatorAddress] ?? new TransactionFeeDelegatees();
        var allDelegateesMap = allDelegatees.Delegatees;

        var delegateeAddress = Context.Sender.ToBase58();
        var delegationsToInput = input.Delegations;

        var currentHeight = Context.CurrentHeight;

        // No this delegatee, init it, pour all available delegations in, and add it.
        if (!allDelegateesMap.ContainsKey(delegateeAddress))
        {
            // If there has been already DELEGATEE_MAX_COUNT delegatees, and still try to add，fail.
            if (allDelegateesMap.Count() >= TokenContractConstants.DELEGATEE_MAX_COUNT)
            {
                return new SetTransactionFeeDelegationsOutput()
                {
                    Success = false
                };
            }

            // At least one delegation to input, else, no need to add a new one.
            if (delegationsToInput.Any(x => x.Value > 0))
            {
                allDelegateesMap.Add(delegateeAddress, new TransactionFeeDelegations());

                // pour all >0 delegations in
                foreach (var (key, value) in delegationsToInput)
                {
                    if (value > 0)
                    {
                        AssertValidToken(key, value);
                        allDelegateesMap[delegateeAddress].Delegations.Add(key, value);
                    }
                }

                allDelegateesMap[delegateeAddress].BlockHeight = currentHeight;

                // Set and Fire logEvent
                State.TransactionFeeDelegateesMap[input.DelegatorAddress] = allDelegatees;
                Context.Fire(new TransactionFeeDelegationAdded()
                {
                    Caller = Context.Sender,
                    Delegatee = Context.Sender,
                    Delegator = input.DelegatorAddress
                });
            }
        }
        else // This delegatee exists, so update
        {
            var delegationsMap = allDelegateesMap[delegateeAddress].Delegations;
            foreach (var (key, value) in delegationsToInput)
            {
                if (value <= 0 && delegationsMap.ContainsKey(key))
                {
                    delegationsMap.Remove(key);
                }
                else if (value > 0)
                {
                    AssertValidToken(key, value);
                    delegationsMap[key] = value;
                }
            }

            // Set and Fire logEvent
            State.TransactionFeeDelegateesMap[input.DelegatorAddress] = allDelegatees;

            // If a delegatee has no delegations, remove it!
            if (allDelegateesMap[delegateeAddress].Delegations.Count != 0)
                return new SetTransactionFeeDelegationsOutput()
                {
                    Success = true
                };
            State.TransactionFeeDelegateesMap[input.DelegatorAddress].Delegatees.Remove(delegateeAddress);
            Context.Fire(new TransactionFeeDelegationCancelled()
            {
                Caller = Context.Sender,
                Delegatee = Context.Sender,
                Delegator = input.DelegatorAddress
            });
        }

        return new SetTransactionFeeDelegationsOutput()
        {
            Success = true
        };
    }

    public override Empty RemoveTransactionFeeDelegator(
        RemoveTransactionFeeDelegatorInput input)
    {
        Assert(input.DelegatorAddress != null, "Delegator Address cannot be null!");

        if (State.TransactionFeeDelegateesMap[input.DelegatorAddress] == null)
        {
            return new Empty();
        }

        if (!State.TransactionFeeDelegateesMap[input.DelegatorAddress].Delegatees
                .ContainsKey(Context.Sender.ToBase58()))
        {
            return new Empty();
        }

        var delegatees = State.TransactionFeeDelegateesMap[input.DelegatorAddress];
        delegatees.Delegatees.Remove(Context.Sender.ToBase58());
        State.TransactionFeeDelegateesMap[input.DelegatorAddress] = delegatees;

        Context.Fire(new TransactionFeeDelegationCancelled
        {
            Caller = Context.Sender,
            Delegatee = Context.Sender,
            Delegator = input.DelegatorAddress
        });
        return new Empty();
    }

    public override Empty RemoveTransactionFeeDelegatee(
        RemoveTransactionFeeDelegateeInput input)
    {
        Assert(input.DelegateeAddress != null, "Delegatee Address cannot be null!");

        if (State.TransactionFeeDelegateesMap[Context.Sender] == null)
        {
            return new Empty();
        }

        if (!State.TransactionFeeDelegateesMap[Context.Sender].Delegatees
                .ContainsKey(input.DelegateeAddress.ToBase58()))
        {
            return new Empty();
        }

        var delegatees = State.TransactionFeeDelegateesMap[Context.Sender];
        delegatees.Delegatees.Remove(input.DelegateeAddress.ToBase58());
        State.TransactionFeeDelegateesMap[Context.Sender] = delegatees;

        Context.Fire(new TransactionFeeDelegationCancelled
        {
            Caller = Context.Sender,
            Delegatee = input.DelegateeAddress,
            Delegator = Context.Sender
        });
        return new Empty();
    }

    public override TransactionFeeDelegations GetTransactionFeeDelegationsOfADelegatee(
        GetTransactionFeeDelegationsOfADelegateeInput input)
    {
        var allDelegatees = State.TransactionFeeDelegateesMap[input.DelegatorAddress];
        var delegateeAddress = input.DelegateeAddress.ToBase58();
        // According to protoBuf, return an empty object, but null.
        if (allDelegatees == null)
        {
            return new TransactionFeeDelegations();
        }

        var allDelegateesMap = allDelegatees.Delegatees;
        return allDelegateesMap.ContainsKey(delegateeAddress)
            ? allDelegateesMap[delegateeAddress]
            : new TransactionFeeDelegations();
    }

    public override GetTransactionFeeDelegateesOutput GetTransactionFeeDelegatees(
        GetTransactionFeeDelegateesInput input)
    {
        Assert(input != null && input!.DelegatorAddress != null, "invalid input");
        var allDelegatees = State.TransactionFeeDelegateesMap[input.DelegatorAddress];

        if (allDelegatees == null || allDelegatees.Delegatees == null || allDelegatees.Delegatees.Count == 0)
        {
            return new GetTransactionFeeDelegateesOutput();
        }

        return new GetTransactionFeeDelegateesOutput
        {
            DelegateeAddresses = { allDelegatees.Delegatees.Keys.Select(Address.FromBase58) }
        };
    }

    public override Empty SetTransactionFeeDelegateInfos(SetTransactionFeeDelegateInfosInput input)
    {
        Assert(input.DelegatorAddress != null && input.DelegateInfoList.Count > 0,
            "Delegator address and delegate info cannot be null.");
        var toAddTransactionList = new DelegateTransactionList();
        var toUpdateTransactionList = new DelegateTransactionList();
        var toCancelTransactionList = new DelegateTransactionList();
        var delegatorAddress = input.DelegatorAddress;
        foreach (var delegateInfo in input.DelegateInfoList)
        {
            //If isUnlimitedDelegate is false,delegate info list should > 0.
            Assert(delegateInfo.IsUnlimitedDelegate || delegateInfo.Delegations.Count > 0,
                "Delegation cannot be null.");
            Assert(delegateInfo.ContractAddress != null && !string.IsNullOrEmpty(delegateInfo.MethodName),
                "Invalid contract address and method name.");

            var existDelegateeInfoList =
                State.TransactionFeeDelegateInfoMap[delegatorAddress][delegateInfo.ContractAddress]
                    [delegateInfo.MethodName] ?? new TransactionFeeDelegatees();
            var delegateeAddress = Context.Sender.ToBase58();
            var existDelegateeList = existDelegateeInfoList.Delegatees;
            //If the transaction contains delegatee,update delegate info.
            if (existDelegateeList.TryGetValue(delegateeAddress, out var value))
            {
                toUpdateTransactionList.Value.Add(UpdateDelegateInfo(value, delegateInfo));
            } //else,add new delegate info.
            else
            {
                Assert(existDelegateeList.Count < TokenContractConstants.DELEGATEE_MAX_COUNT,
                    "The quantity of delegatee has reached its limit");
                existDelegateeList.Add(delegateeAddress, new TransactionFeeDelegations());
                var transactionFeeDelegations = existDelegateeList[delegateeAddress];
                toAddTransactionList.Value.Add(AddDelegateInfo(transactionFeeDelegations, delegateInfo));
            }

            if (existDelegateeInfoList.Delegatees[delegateeAddress].Delegations.Count == 0 &&
                !existDelegateeInfoList.Delegatees[delegateeAddress].IsUnlimitedDelegate)
            {
                existDelegateeInfoList.Delegatees.Remove(delegateeAddress);
                toCancelTransactionList.Value.Add(new DelegateTransaction
                {
                    ContractAddress = delegateInfo.ContractAddress,
                    MethodName = delegateInfo.MethodName
                });
            }

            State.TransactionFeeDelegateInfoMap[delegatorAddress][delegateInfo.ContractAddress]
                [delegateInfo.MethodName] = existDelegateeInfoList;
        }

        FireLogEvent(toAddTransactionList, toUpdateTransactionList, toCancelTransactionList, delegatorAddress);

        return new Empty();
    }

    private DelegateTransaction AddDelegateInfo(TransactionFeeDelegations existDelegateeList, DelegateInfo delegateInfo)
    {
        if (!delegateInfo.IsUnlimitedDelegate)
        {
            foreach (var (symbol, amount) in delegateInfo.Delegations)
            {
                AssertValidToken(symbol, amount);
                existDelegateeList.Delegations[symbol] = amount;
            }
        }
        existDelegateeList.BlockHeight = Context.CurrentHeight;
        existDelegateeList.IsUnlimitedDelegate = delegateInfo.IsUnlimitedDelegate;
        return new DelegateTransaction
        {
            ContractAddress = delegateInfo.ContractAddress,
            MethodName = delegateInfo.MethodName
        };
    }

    private DelegateTransaction UpdateDelegateInfo(TransactionFeeDelegations existDelegateInfo, DelegateInfo delegateInfo)
    {
        var existDelegation = existDelegateInfo.Delegations;
        if (delegateInfo.IsUnlimitedDelegate)
        {
            existDelegation.Clear();
        }
        else
        {
            var delegation = delegateInfo.Delegations;
            foreach (var (symbol, amount) in delegation)
            {
                if (existDelegation.ContainsKey(symbol))
                {
                    if (amount <= 0)
                    {
                        existDelegation.Remove(symbol);
                    }
                    else
                    {
                        AssertValidToken(symbol, amount);
                        existDelegation[symbol] = amount;
                    }
                }
                else
                {
                    AssertValidToken(symbol, amount);
                    existDelegation[symbol] = amount;
                }
            }
        }

        existDelegateInfo.BlockHeight = Context.CurrentHeight;
        existDelegateInfo.IsUnlimitedDelegate = delegateInfo.IsUnlimitedDelegate;
        return new DelegateTransaction
        {
            ContractAddress = delegateInfo.ContractAddress,
            MethodName = delegateInfo.MethodName
        };
    }

    private void FireLogEvent(DelegateTransactionList toAddTransactionList,
        DelegateTransactionList toUpdateTransactionList, DelegateTransactionList toCancelTransactionList,
        Address delegatorAddress)
    {
        if (toAddTransactionList.Value.Count > 0)
        {
            Context.Fire(new TransactionFeeDelegateInfoAdded
            {
                Caller = Context.Sender,
                Delegatee = Context.Sender,
                Delegator = delegatorAddress,
                DelegateTransactionList = toAddTransactionList
            });
        }

        if (toUpdateTransactionList.Value.Count > 0)
        {
            Context.Fire(new TransactionFeeDelegateInfoUpdated
            {
                Caller = Context.Sender,
                Delegatee = Context.Sender,
                Delegator = delegatorAddress,
                DelegateTransactionList = toUpdateTransactionList
            });
        }

        if (toCancelTransactionList.Value.Count > 0)
        {
            Context.Fire(new TransactionFeeDelegateInfoCancelled
            {
                Caller = Context.Sender,
                Delegatee = Context.Sender,
                Delegator = delegatorAddress,
                DelegateTransactionList = toCancelTransactionList
            });
        }
    }

    public override Empty RemoveTransactionFeeDelegateeInfos(RemoveTransactionFeeDelegateeInfosInput input)
    {
        Assert(input.DelegateeAddress != null, "Delegatee address cannot be null.");
        Assert(input.DelegateTransactionList.Count > 0, "Delegate transaction list should not be null.");
        var delegatorAddress = Context.Sender;
        var delegateeAddress = input.DelegateeAddress?.ToBase58();
        RemoveTransactionFeeDelegateInfo(input.DelegateTransactionList.ToList(), delegatorAddress, delegateeAddress);
        return new Empty();
    }

    public override Empty RemoveTransactionFeeDelegatorInfos(RemoveTransactionFeeDelegatorInfosInput input)
    {
        Assert(input.DelegatorAddress != null, "Delegator address cannot be null.");
        Assert(input.DelegateTransactionList.Count > 0, "Delegate transaction list should not be null.");
        var delegateeAddress = Context.Sender.ToBase58();
        var delegatorAddress = input.DelegatorAddress;
        RemoveTransactionFeeDelegateInfo(input.DelegateTransactionList.ToList(), delegatorAddress, delegateeAddress);
        return new Empty();
    }

    private void RemoveTransactionFeeDelegateInfo(List<DelegateTransaction> delegateTransactionList,Address delegatorAddress,string delegateeAddress)
    {
        var toCancelTransactionList = new DelegateTransactionList();
        foreach (var delegateTransaction in delegateTransactionList.Distinct())
        {
            Assert(delegateTransaction.ContractAddress != null && !string.IsNullOrEmpty(delegateTransaction.MethodName),
                "Invalid contract address and method name.");

            var delegateeInfo =
                State.TransactionFeeDelegateInfoMap[delegatorAddress][delegateTransaction.ContractAddress][
                    delegateTransaction.MethodName];
            if (delegateeInfo == null || !delegateeInfo.Delegatees.ContainsKey(delegateeAddress)) continue;
            delegateeInfo.Delegatees.Remove(delegateeAddress);
            toCancelTransactionList.Value.Add(delegateTransaction);
            State.TransactionFeeDelegateInfoMap[delegatorAddress][delegateTransaction.ContractAddress][
                delegateTransaction.MethodName] = delegateeInfo;
        }

        if (toCancelTransactionList.Value.Count > 0)
        {
            Context.Fire(new TransactionFeeDelegateInfoCancelled
            {
                Caller = Context.Sender,
                Delegator = delegatorAddress,
                Delegatee = Address.FromBase58(delegateeAddress),
                DelegateTransactionList = toCancelTransactionList
            });
        }
    }

    public override GetTransactionFeeDelegateeListOutput GetTransactionFeeDelegateeList(
        GetTransactionFeeDelegateeListInput input)
    {
        Assert(input.DelegatorAddress != null && input.ContractAddress != null && input.MethodName != null,
            "Invalid input.");
        var allDelegatees =
            State.TransactionFeeDelegateInfoMap[input.DelegatorAddress][input.ContractAddress][input.MethodName];

        if (allDelegatees?.Delegatees == null || allDelegatees.Delegatees.Count == 0)
        {
            return new GetTransactionFeeDelegateeListOutput();
        }

        return new GetTransactionFeeDelegateeListOutput
        {
            DelegateeAddresses = { allDelegatees.Delegatees.Keys.Select(Address.FromBase58) }
        };
    }

    public override TransactionFeeDelegations GetTransactionFeeDelegateInfo(
        GetTransactionFeeDelegateInfoInput input)
    {
        var allDelegatees =
            State.TransactionFeeDelegateInfoMap[input.DelegatorAddress][input.ContractAddress][input.MethodName];
        var delegateeAddress = input.DelegateeAddress.ToBase58();
        // According to protoBuf, return an empty object, but null.
        if (allDelegatees == null) return new TransactionFeeDelegations();

        var allDelegateesMap = allDelegatees.Delegatees;
        var result = allDelegateesMap.TryGetValue(delegateeAddress, out var value);
        return result ? value : new TransactionFeeDelegations();
    }
}
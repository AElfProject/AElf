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
}
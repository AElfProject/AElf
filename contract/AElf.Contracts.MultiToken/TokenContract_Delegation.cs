using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS1;
using AElf.Standards.ACS10;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Contracts.MultiToken;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    private const int DELEGATEE_MAX_COUNT = 128;
     
    public override SetTransactionFeeDelegationsOutput SetTransactionFeeDelegations(
        SetTransactionFeeDelegationsInput input)
    {
        Assert(input.Delegations != null, "Delegations cannot be null!");
        
        // get all delegatees, init it if null.
        var allDelegatees = State.DelegateesMap[input.DelegatorAddress] ?? new TransactionFeeDelegatees();
        var alldelegateesMap = allDelegatees.Delegatees;

        var delegateeAddress = Context.Sender.ToBase58();
        var delegationsToInput = input.Delegations;

        // No this delegatee, init it, pour all available delegations in, and add it.
        if (!alldelegateesMap.ContainsKey(delegateeAddress))
        {
            // If there has been already DELEGATEE_MAX_COUNT delegatees, and still try to add，fail.
            if (alldelegateesMap.Count() >= DELEGATEE_MAX_COUNT
                && delegationsToInput.All(x => x.Value > 0))
            {
                return new SetTransactionFeeDelegationsOutput()
                {
                    Success = false
                };
            }
            alldelegateesMap.Add(delegateeAddress, new TransactionFeeDelegations());

            // pour all >0 delegations in
            foreach (var (key, value) in delegationsToInput)
            {
                if (value > 0)
                {
                    AssertValidToken(key, value);
                    alldelegateesMap[delegateeAddress].Delegations.Add(key,value);
                }
            }
            
            // Set and Fire logEvent
            State.DelegateesMap[input.DelegatorAddress] = allDelegatees;
            Context.Fire(new TransactionFeeDelegationAdded()
            {
                Caller = Context.Sender,
                Delegatee = Context.Sender,
                Delegator = input.DelegatorAddress
            });
        }
        else // This delegatee exists, so update
        {
            var delegationsMap = alldelegateesMap[delegateeAddress].Delegations;
            foreach (var (key, value) in delegationsToInput)
            {
                if (value <= 0 && delegationsMap.ContainsKey(key))
                {
                    delegationsMap.Remove(key);
                }
                else if(value > 0)
                {
                    AssertValidToken(key, value);
                    delegationsMap[key] = value;
                }
            }
            
            // Set and Fire logEvent
            State.DelegateesMap[input.DelegatorAddress] = allDelegatees;
            
            // If a delegatee has no delegations, remove it!
            if (alldelegateesMap[delegateeAddress].Delegations.Count != 0)
                return new SetTransactionFeeDelegationsOutput()
                {
                    Success = true
                };
            State.DelegateesMap[input.DelegatorAddress].Delegatees.Remove(delegateeAddress);
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
        
        if (State.DelegateesMap[input.DelegatorAddress] == null)
        {
            return new Empty();
        }

        if (!State.DelegateesMap[input.DelegatorAddress].Delegatees.ContainsKey(Context.Sender.ToBase58()))
        {
            return new Empty();
        }

        var delegatees = State.DelegateesMap[input.DelegatorAddress];
        delegatees.Delegatees.Remove(Context.Sender.ToBase58());
        State.DelegateesMap[input.DelegatorAddress] = delegatees;

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
        
        if (State.DelegateesMap[Context.Sender] == null)
        {
            return new Empty();
        }

        if (!State.DelegateesMap[Context.Sender].Delegatees.ContainsKey(input.DelegateeAddress.ToBase58()))
        {
            return new Empty();
        }

        var delegatees = State.DelegateesMap[Context.Sender];
        delegatees.Delegatees.Remove(input.DelegateeAddress.ToBase58());
        State.DelegateesMap[Context.Sender] = delegatees;

        Context.Fire(new TransactionFeeDelegationCancelled
        {
            Caller = Context.Sender,
            Delegatee = input.DelegateeAddress,
            Delegator = Context.Sender
        });
        return new Empty();
    }

    public override TransactionFeeDelegations GetTransactionFeeDelegationsOfADelegatee(GetTransactionFeeDelegationsOfADelegateeInput input)
    {
        var allDelegatees = State.DelegateesMap[input.DelegatorAddress];
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
}
ACS8 - Transaction Resource Token Fee Standard
==============================================

ACS8 has some similarities to ACS1, both of them are charge transaction
fee standard.

The difference is that ACS1 charges the user a transaction fee, ACS8
charges the called contract, and the transaction fee charged by ACS8 is
the specified four tokens: WRITE, READ, NET, TRAFFIC.

In another word, if a contract declares that it inherits from ACS8, each
transaction in this contract will charge four kinds of resource token.

Interface
---------

Only one method is defined in the acs8.proto file:

Methods
~~~~~~~

+--------------------+----------------------------------------------------------------+------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Method Name        | Request Type                                                   | Response Type                                        | Description                                                                                                                                                                                              |
+====================+================================================================+======================================================+==========================================================================================================================================================================================================+
| BuyResourceToken   | `acs8.BuyResourceTokenInput <#acs8.BuyResourceTokenInput>`__   | `google.protobuf.Empty <#google.protobuf.Empty>`__   | Buy one of the four resource coins, which consumes the ELF balance in the contract account (you can recharge it yourself, or you can collect the user’s ELF tokens as a profit to be self-sufficient).   |
+--------------------+----------------------------------------------------------------+------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+

Types
~~~~~

.. raw:: html

   <div id="acs8.BuyResourceTokenInput">

.. raw:: html

   </div>

acs8.BuyResourceTokenInput
^^^^^^^^^^^^^^^^^^^^^^^^^^

+--------------+------------------------+------------------------------------------------------------------------------------------------------------------+---------+
| Field        | Type                   | Description                                                                                                      | Label   |
+==============+========================+==================================================================================================================+=========+
| symbol       | `string <#string>`__   | The symbol token you want to buy.                                                                                |         |
+--------------+------------------------+------------------------------------------------------------------------------------------------------------------+---------+
| amount       | `int64 <#int64>`__     | The amount you want to buy.                                                                                      |         |
+--------------+------------------------+------------------------------------------------------------------------------------------------------------------+---------+
| pay\_limit   | `int64 <#int64>`__     | Limit of cost. If the token required for buy exceeds this value, the buy will be abandoned. And 0 is no limit.   |         |
+--------------+------------------------+------------------------------------------------------------------------------------------------------------------+---------+

Usage
-----

The contract inherited from ACS1 uses a pre-plugin transaction called
``ChargeTransactionFees`` for charging transaction fee.

Because the specific charge amount is determined by the actual
consumption of the transaction, the post-plugin generates
``ChargeResourceToken`` transaction to charge resource token.

The implementation of ``ChargeResourceToken`` is also similar to it of
``ChargeTransactionFees``:

.. code:: c#

   public override Empty ChargeResourceToken(ChargeResourceTokenInput input)
   {
       Context.LogDebug(() => string.Format("Start executing ChargeResourceToken.{0}", input));
       if (input.Equals(new ChargeResourceTokenInput()))
       {
           return new Empty();
       }
       var bill = new TransactionFeeBill();
       foreach (var pair in input.CostDic)
       {
           Context.LogDebug(() => string.Format("Charging {0} {1} tokens.", pair.Value, pair.Key));
           var existingBalance = GetBalance(Context.Sender, pair.Key);
           Assert(existingBalance >= pair.Value,
               string.Format("Insufficient resource of {0}. Need balance: {1}; Current balance: {2}.", pair.Key, pair.Value, existingBalance));
           bill.FeesMap.Add(pair.Key, pair.Value);
       }
       foreach (var pair in bill.FeesMap)
       {
           Context.Fire(new ResourceTokenCharged
           {
               Symbol = pair.Key,
               Amount = pair.Value,
               ContractAddress = Context.Sender
           });
           if (pair.Value == 0)
           {
               Context.LogDebug(() => string.Format("Maybe incorrect charged resource fee of {0}: it's 0.", pair.Key));
           }
       }
       return new Empty();
   }

The amount of each resource token should be calculated by
``AElf.Kernel.FeeCalculation``. In detail, A data structure named
``CalculateFeeCoefficients`` is defined in token_contract.proto, whose
function is to save all coefficients of a polynomial, and every three
coefficients are a group, such as a, b, c, which means (b / c) \* x ^ a.
Each resource token has a polynomial that calculates it. Then according
to the polynomial and the actual consumption of the resource, calculate
the cost of the resource token. Finally, the cost is used as the
parameter of ``ChargeResourceToken`` to generate this post-plugin
transaction.

In addition, the method of the contract that has been owed cannot be
executed before the contract top up resource token. As a result, a
pre-plugin transaction is added, similar to the ACS5 pre-plugin
transaction, which checks the contract’s resource token balance, and the
transaction’s method name is ``CheckResourceToken`` :

.. code:: c#

   public override Empty CheckResourceToken(Empty input)
   {
       foreach (var symbol in Context.Variables.GetStringArray(TokenContractConstants.PayTxFeeSymbolListName))
       {
           var balance = GetBalance(Context.Sender, symbol);
           var owningBalance = State.OwningResourceToken[Context.Sender][symbol];
           Assert(balance > owningBalance,
               string.Format("Contract balance of {0} token is not enough. Owning {1}.", symbol, owningBalance));
       }
       return new Empty();
   }

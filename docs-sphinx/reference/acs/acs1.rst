ACS1 - Transaction Fee Standard
===============================

ACS1 is used to manage the transfer fee.

Interface
---------

The contract inherited from ACS1 need implement the APIs below:

Methods
~~~~~~~

+-----------------------------+------------------------------------------------------------------+------------------------------------------------------+------------------------------------------------------------------------------------------------------+
| Method Name                 | Request Type                                                     | Response Type                                        | Description                                                                                          |
+=============================+==================================================================+======================================================+======================================================================================================+
| SetMethodFee                | `acs1.MethodFees <#acs1.MethodFees>`__                           | `google.protobuf.Empty <#google.protobuf.Empty>`__   | Set the method fees for the specified method. Note that this will override all fees of the method.   |
+-----------------------------+------------------------------------------------------------------+------------------------------------------------------+------------------------------------------------------------------------------------------------------+
| ChangeMethodFeeController   | `AuthorityInfo <#AuthorityInfo>`__                               | `google.protobuf.Empty <#google.protobuf.Empty>`__   | Change the method fee controller, the default is parliament and default organization.                |
+-----------------------------+------------------------------------------------------------------+------------------------------------------------------+------------------------------------------------------------------------------------------------------+
| GetMethodFee                | `google.protobuf.StringValue <#google.protobuf.StringValue>`__   | `acs1.MethodFees <#acs1.MethodFees>`__               | Query method fee information by method name.                                                         |
+-----------------------------+------------------------------------------------------------------+------------------------------------------------------+------------------------------------------------------------------------------------------------------+
| GetMethodFeeController      | `google.protobuf.Empty <#google.protobuf.Empty>`__               | `AuthorityInfo <#AuthorityInfo>`__                   | Query the method fee controller.                                                                     |
+-----------------------------+------------------------------------------------------------------+------------------------------------------------------+------------------------------------------------------------------------------------------------------+

Types
~~~~~

.. raw:: html

   <div id="acs1.MethodFee">

.. raw:: html

   </div>

acs1.MethodFee
^^^^^^^^^^^^^^

+--------------+------------------------+---------------------------------------+---------+
| Field        | Type                   | Description                           | Label   |
+==============+========================+=======================================+=========+
| symbol       | `string <#string>`__   | The token symbol of the method fee.   |         |
+--------------+------------------------+---------------------------------------+---------+
| basic\_fee   | `int64 <#int64>`__     | The amount of fees to be charged.     |         |
+--------------+------------------------+---------------------------------------+---------+

.. raw:: html

   <div id="acs1.MethodFees">

.. raw:: html

   </div>

acs1.MethodFees
^^^^^^^^^^^^^^^

+-----------------------+-----------------------------------+----------------------------------------------------------------+------------+
| Field                 | Type                              | Description                                                    | Label      |
+=======================+===================================+================================================================+============+
| method\_name          | `string <#string>`__              | The name of the method to be charged.                          |            |
+-----------------------+-----------------------------------+----------------------------------------------------------------+------------+
| fees                  | `MethodFee <#acs1.MethodFee>`__   | List of fees to be charged.                                    | repeated   |
+-----------------------+-----------------------------------+----------------------------------------------------------------+------------+
| is\_size\_fee\_free   | `bool <#bool>`__                  | Optional based on the implementation of SetMethodFee method.   |            |
+-----------------------+-----------------------------------+----------------------------------------------------------------+------------+

.. raw:: html

   <div id="AuthorityInfo">

.. raw:: html

   </div>

AuthorityInfo
^^^^^^^^^^^^^

+---------------------+------------------------------------+---------------------------------------------+---------+
| Field               | Type                               | Description                                 | Label   |
+=====================+====================================+=============================================+=========+
| contract\_address   | `aelf.Address <#aelf.Address>`__   | The contract address of the controller.     |         |
+---------------------+------------------------------------+---------------------------------------------+---------+
| owner\_address      | `aelf.Address <#aelf.Address>`__   | The address of the owner of the contract.   |         |
+---------------------+------------------------------------+---------------------------------------------+---------+

Attention: just the system contract on main chain is able to implement
acs1.

Usage
-----

On AElf, a pre-transaction is generated by pre-plugin
``FeeChargePreExecutionPlugin`` before the transaction main processing.
It is used to charge the transaction fee.

The generated transaction’s method is ``ChargeTransactionFees``. The
implementation is roughly like that (part of the code is omitted):

.. code:: c#

   /// <summary>
   /// Related transactions will be generated by acs1 pre-plugin service,
   /// and will be executed before the origin transaction.
   /// </summary>
   /// <param name="input"></param>
   /// <returns></returns>
   public override BoolValue ChargeTransactionFees(ChargeTransactionFeesInput input)
   {
       // ...
       // Record tx fee bill during current charging process.
       var bill = new TransactionFeeBill();
       var fromAddress = Context.Sender;
       var methodFees = Context.Call<MethodFees>(input.ContractAddress, nameof(GetMethodFee),
           new StringValue {Value = input.MethodName});
       var successToChargeBaseFee = true;
       if (methodFees != null && methodFees.Fees.Any())
       {
           successToChargeBaseFee = ChargeBaseFee(GetBaseFeeDictionary(methodFees), ref bill);
       }
       var successToChargeSizeFee = true;
       if (!IsMethodFeeSetToZero(methodFees))
       {
           // Then also do not charge size fee.
           successToChargeSizeFee = ChargeSizeFee(input, ref bill);
       }
       // Update balances.
       foreach (var tokenToAmount in bill.FeesMap)
       {
           ModifyBalance(fromAddress, tokenToAmount.Key, -tokenToAmount.Value);
           Context.Fire(new TransactionFeeCharged
           {
               Symbol = tokenToAmount.Key,
               Amount = tokenToAmount.Value
           });
           if (tokenToAmount.Value == 0)
           {
               //Context.LogDebug(() => $"Maybe incorrect charged tx fee of {tokenToAmount.Key}: it's 0.");
           }
       }
       return new BoolValue {Value = successToChargeBaseFee && successToChargeSizeFee};
   }

In this method, the transaction fee consists of two parts:

1. The system calls ``GetMethodFee``\ (line 15) to get the transacion
   fee you should pay. Then, it will check whether your balance is
   enough. If your balance is sufficient, the fee will be signed in the
   bill (variant bill). If not, your transaction will be rejected.

2. If the method fee is not set to 0 by the contract developer, the
   system will charge size fee. (the size if calculate by the
   parameter’s size)

After charging successfully, an ``TransactionFeeCharged`` event is
thrown, and the balance of the sender is modified.

The ``TransactionFeeCharged`` event will be captured and processed on
the chain to calculate the total amount of transaction fees charged in
the block. In the next block, the 10% of the transaction fee charged in
this block is destroyed, the remaining 90% flows to dividend pool on the
main chain, and is transferred to the ``FeeReciever`` on the side chain.
The code is:

.. code:: c#

   /// <summary>
   /// Burn 10% of tx fees.
   /// If Side Chain didn't set FeeReceiver, burn all.
   /// </summary>
   /// <param name="symbol"></param>
   /// <param name="totalAmount"></param>
   private void TransferTransactionFeesToFeeReceiver(string symbol, long totalAmount)
   {
       Context.LogDebug(() => "Transfer transaction fee to receiver.");
       if (totalAmount <= 0) return;
       var burnAmount = totalAmount.Div(10);
       if (burnAmount > 0)
           Context.SendInline(Context.Self, nameof(Burn), new BurnInput
           {
               Symbol = symbol,
               Amount = burnAmount
           });
       var transferAmount = totalAmount.Sub(burnAmount);
       if (transferAmount == 0)
           return;
       var treasuryContractAddress =
           Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
       if ( treasuryContractAddress!= null)
       {
           // Main chain would donate tx fees to dividend pool.
           if (State.DividendPoolContract.Value == null)
               State.DividendPoolContract.Value = treasuryContractAddress;
           State.DividendPoolContract.Donate.Send(new DonateInput
           {
               Symbol = symbol,
               Amount = transferAmount
           });
       }
       else
       {
           if (State.FeeReceiver.Value != null)
           {
               Context.SendInline(Context.Self, nameof(Transfer), new TransferInput
               {
                   To = State.FeeReceiver.Value,
                   Symbol = symbol,
                   Amount = transferAmount,
               });
           }
           else
           {
               // Burn all!
               Context.SendInline(Context.Self, nameof(Burn), new BurnInput
               {
                   Symbol = symbol,
                   Amount = transferAmount
               });
           }
       }
   }

In this way, AElf charges the transaction fee via the ``GetMethodFee``
provided by ACS1, and the other three methods are used to help with the
implementations of GetMethodFee.

Implementation
--------------

The easiest way to do this is to just implement the method
``GetMethodFee``.

If there are Foo1, Foo2, Bar1 and Bar2 methods related to business logic
in a contract, they are priced as 1, 1, 2, 2 ELF respectively, and the
transaction fees of these four methods will not be easily modified
later, they can be implemented as follows:

.. code:: c#

   public override MethodFees GetMethodFee(StringValue input)
   {
       if (input.Value == nameof(Foo1) || input.Value == nameof(Foo2))
       {
           return new MethodFees
           {
               MethodName = input.Value,
               Fees =
               {
                   new MethodFee
                   {
                       BasicFee = 1_00000000,
                       Symbol = Context.Variables.NativeSymbol
                   }
               }
           };
       }
       if (input.Value == nameof(Bar1) || input.Value == nameof(Bar2))
       {
           return new MethodFees
           {
               MethodName = input.Value,
               Fees =
               {
                   new MethodFee
                   {
                       BasicFee = 2_00000000,
                       Symbol = Context.Variables.NativeSymbol
                   }
               }
           };
       }
       return new MethodFees();
   }

This implementation can modify the transaction fee only by upgrading the
contract, without implementing the other three interfaces.

A more recommended implementation needs to define an ``MappedState`` in
the State file for the contract:

.. code:: c#

   public MappedState<string, MethodFees> TransactionFees { get; set; }

Modify the ``TransactionFees`` data structure in the ``SetMethodFee``
method, and return the value in the ``GetMethodFee`` method.

In this solution, the implementation of GetMethodFee is very easy:

.. code:: c#

   public override MethodFees GetMethodFee(StringValue input)
       return State.TransactionFees[input.Value];
   }

The implementation of ``SetMethodFee`` requires the addition of
permission management, since contract developers don’t want the
transaction fees of their contract methods to be arbitrarily modified by
others.

Referring to the ``MultiToken contract``, it can be implemented as
follows:

Firstly, define a ``SingletonState`` with type ``AuthorityInfo``\ (in
authority_info.proto)

.. code:: c#

   public SingletonState<AuthorityInfo> MethodFeeController { get; set; }

Then, check the sender’s right by comparing its address with owner.

.. code:: c#

   public override Empty SetMethodFee(MethodFees input)
   {
     foreach (var symbolToAmount in input.Fees)
     {
        AssertValidToken(symbolToAmount.Symbol, symbolToAmount.BasicFee); 
     }
     RequiredMethodFeeControllerSet();
     Assert(Context.Sender ==             State.MethodFeeController.Value.OwnerAddress, "Unauthorized to set method fee.");
       State.TransactionFees[input.MethodName] = input;
       return new Empty();
   }

AssertValidToken checks if the token symbol exists, and the ``BasicFee``
is reasonable.

The permission check code is in the lines 8 and 9, and
``RequiredMethodFeeControllerSet`` prevents the permission is not set
before.

If permissions are not set, the ``SetMethodFee`` method can only be
called by the default address of the Parliament organization. If a
method is sent through this organization, it means that two-thirds of
the block producers have agreed to the proposal.

.. code:: c#

   private void RequiredMethodFeeControllerSet()
   {
      if (State.MethodFeeController.Value != null) return;
      if (State.ParliamentContract.Value == null)
      {
        State.ParliamentContract.Value =         Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
      }
      var defaultAuthority = new AuthorityInfo();
      // Parliament Auth Contract maybe not deployed.
      if (State.ParliamentContract.Value != null)
      {
        defaultAuthority.OwnerAddress =               State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
        defaultAuthority.ContractAddress = State.ParliamentContract.Value;
      }
      State.MethodFeeController.Value = defaultAuthority;
   }

Of course, the authority of ``SetMethodFee`` can also be changed,
provided that the transaction to modify the authority is sent from the
default address of the ``Parliament contract``:

.. code:: c#

   public override Empty ChangeMethodFeeController(AuthorityInfo input)
   {
       RequiredMethodFeeControllerSet();
       AssertSenderAddressWith(State.MethodFeeController.Value.OwnerAddress);
       var organizationExist = CheckOrganizationExist(input);
       Assert(organizationExist, "Invalid authority input.");
       State.MethodFeeController.Value = input;
       return new Empty();
   }

The implementation of ``GetMethodFeeController`` is also very easy：

.. code:: c#

   public override AuthorityInfo GetMethodFeeController(Empty input)
   {
       RequiredMethodFeeControllerSet();
       return State.MethodFeeController.Value;
   }

Above all, these are the two ways to implement acs1. Mostly,
implementations will use a mixture of the two: part of methods’ fee is
set with a fixed value, the other part of method is not to set method
fee.

Test
----

Create ACS1’s Stub, and call ``GetMethodFee`` and
``GetMethodFeeController`` to check if the return value is expected.

Example
-------

All AElf system contracts implement ACS1, which can be used as a
reference.

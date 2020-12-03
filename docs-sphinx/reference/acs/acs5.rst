ACS5 - Contract Threshold Standard
==================================

If you want to raise the threshold for using contract, consider
implementing ACS5.

Interface
---------

To limit to call a method in a contract, you only need to implement 
the following five interfaces:

Methods
~~~~~~~

+-----------------------------+----------------------------------------------------------------------------------+------------------------------------------------------------------+-----------------------------------------+
| Method Name                 | Request Type                                                                     | Response Type                                                    | Description                             |
+=============================+==================================================================================+==================================================================+=========================================+
| SetMethodCallingThreshold   | `acs5.SetMethodCallingThresholdInput <#acs5.SetMethodCallingThresholdInput>`__   | `google.protobuf.Empty <#google.protobuf.Empty>`__               | Set the threshold for method calling.   |
+-----------------------------+----------------------------------------------------------------------------------+------------------------------------------------------------------+-----------------------------------------+
| GetMethodCallingThreshold   | `google.protobuf.StringValue <#google.protobuf.StringValue>`__                   | `acs5.MethodCallingThreshold <#acs5.MethodCallingThreshold>`__   | Get the threshold for method calling.   |
+-----------------------------+----------------------------------------------------------------------------------+------------------------------------------------------------------+-----------------------------------------+

Types
~~~~~

.. raw:: html

   <div id="acs5.MethodCallingThreshold">

.. raw:: html

   </div>

acs5.MethodCallingThreshold
^^^^^^^^^^^^^^^^^^^^^^^^^^^

+--------------------------+-----------------------------------------------------------------------------------------------------+-------------------------------------------------------------+------------+
| Field                    | Type                                                                                                | Description                                                 | Label      |
+==========================+=====================================================================================================+=============================================================+============+
| symbol\_to\_amount       | `MethodCallingThreshold.SymbolToAmountEntry <#acs5.MethodCallingThreshold.SymbolToAmountEntry>`__   | The threshold for method calling, token symbol -> amount.   | repeated   |
+--------------------------+-----------------------------------------------------------------------------------------------------+-------------------------------------------------------------+------------+
| threshold\_check\_type   | `ThresholdCheckType <#acs5.ThresholdCheckType>`__                                                   | The type of threshold check.                                |            |
+--------------------------+-----------------------------------------------------------------------------------------------------+-------------------------------------------------------------+------------+

.. raw:: html

   <div id="acs5.MethodCallingThreshold.SymbolToAmountEntry">

.. raw:: html

   </div>

acs5.MethodCallingThreshold.SymbolToAmountEntry
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| key     | `string <#string>`__   |               |         |
+---------+------------------------+---------------+---------+
| value   | `int64 <#int64>`__     |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="acs5.SetMethodCallingThresholdInput">

.. raw:: html

   </div>

acs5.SetMethodCallingThresholdInput
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+--------------------------+---------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------------+------------+
| Field                    | Type                                                                                                                | Description                                                 | Label      |
+==========================+=====================================================================================================================+=============================================================+============+
| method                   | `string <#string>`__                                                                                                | The method name to check.                                   |            |
+--------------------------+---------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------------+------------+
| symbol\_to\_amount       | `SetMethodCallingThresholdInput.SymbolToAmountEntry <#acs5.SetMethodCallingThresholdInput.SymbolToAmountEntry>`__   | The threshold for method calling, token symbol -> amount.   | repeated   |
+--------------------------+---------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------------+------------+
| threshold\_check\_type   | `ThresholdCheckType <#acs5.ThresholdCheckType>`__                                                                   | The type of threshold check.                                |            |
+--------------------------+---------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------------+------------+

.. raw:: html

   <div id="acs5.SetMethodCallingThresholdInput.SymbolToAmountEntry">

.. raw:: html

   </div>

acs5.SetMethodCallingThresholdInput.SymbolToAmountEntry
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| key     | `string <#string>`__   |               |         |
+---------+------------------------+---------------+---------+
| value   | `int64 <#int64>`__     |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="acs5.ThresholdCheckType">

.. raw:: html

   </div>

acs5.ThresholdCheckType
^^^^^^^^^^^^^^^^^^^^^^^

+-------------+----------+-------------------------------------------------+
| Name        | Number   | Description                                     |
+=============+==========+=================================================+
| BALANCE     | 0        | Check balance only.                             |
+-------------+----------+-------------------------------------------------+
| ALLOWANCE   | 1        | Check balance and allowance at the same time.   |
+-------------+----------+-------------------------------------------------+


Usage
-----

Similar to ACS1, which uses an automatically generated pre-plugin
transaction called ``ChargeTransactionFees`` to charge a transaction
fee, ACS5 automatically generates a pre-plugin transaction called
``CheckThreshold`` to test whether the account that sent the transaction
can invoke the corresponding method.

The implementation of CheckThreshold:

.. code:: c#

   public override Empty CheckThreshold(CheckThresholdInput input)
   {
       var meetThreshold = false;
       var meetBalanceSymbolList = new List<string>();
       foreach (var symbolToThreshold in input.SymbolToThreshold)
       {
           if (GetBalance(input.Sender, symbolToThreshold.Key) < symbolToThreshold.Value)
               continue;
           meetBalanceSymbolList.Add(symbolToThreshold.Key);
       }
       if (meetBalanceSymbolList.Count > 0)
       {
           if (input.IsCheckAllowance)
           {
               foreach (var symbol in meetBalanceSymbolList)
               {
                   if (State.Allowances[input.Sender][Context.Sender][symbol] <
                       input.SymbolToThreshold[symbol]) continue;
                   meetThreshold = true;
                   break;
               }
           }
           else
           {
               meetThreshold = true;
           }
       }
       if (input.SymbolToThreshold.Count == 0)
       {
           meetThreshold = true;
       }
       Assert(meetThreshold, "Cannot meet the calling threshold.");
       return new Empty();
   }

In other words, if the token balance of the sender of the transaction or
the amount authorized for the target contract does not reach the set
limit, the pre-plugin transaction will throw an exception, thereby it
prevents the original transaction from executing.

Implementation
--------------

Just lik the ``GetMethodFee`` of ACS1, you can implement only one
``GetMethodCallingThreshold`` method.

It can also be achieved by using MappedState<string,
MethodCallingThreshold> in the State class:

.. code:: c#

   public MappedState<string, MethodCallingThreshold> MethodCallingThresholds { get; set; }

But at the same time, do not forget to configure the call permission of
``SetMethodCallingThreshold``, which requires the definition of an Admin
in the State (of course, you can also use ACS3):

.. code:: c#

   public SingletonState<Address> Admin { get; set; }

The easiest implementation：

.. code:: c#

   public override Empty SetMethodCallingThreshold(SetMethodCallingThresholdInput input)
   {
       Assert(State.Admin.Value == Context.Sender, "No permission.");
       State.MethodCallingThresholds[input.Method] = new MethodCallingThreshold
       {
           SymbolToAmount = {input.SymbolToAmount}
       };
       return new Empty();
   }

   public override MethodCallingThreshold GetMethodCallingThreshold(StringValue input)
   {
       return State.MethodCallingThresholds[input.Value];
   }

   public override Empty Foo(Empty input)
   {
       return new Empty();
   }

   message SetMethodCallingThresholdInput {
       string method = 1;
       map<string, int64> symbol_to_amount = 2;// The order matters.
       ThresholdCheckType threshold_check_type = 3;
   }

Test
----

You can test the Foo method defined above.

Make a Stub:

.. code:: c#

   var keyPair = SampleECKeyPairs.KeyPairs[0];
   var acs5DemoContractStub =
       GetTester<ACS5DemoContractContainer.ACS5DemoContractStub>(DAppContractAddress, keyPair);

Before setting the threshold, check the current threshold, which should
be 0:

.. code:: c#

   var methodResult = await acs5DemoContractStub.GetMethodCallingThreshold.CallAsync(
       new StringValue
       {
           Value = nameof(acs5DemoContractStub.Foo)
       });
   methodResult.SymbolToAmount.Count.ShouldBe(0);

The ELF balance of the caller of Foo should be greater than 1 ELF:

.. code:: c#

   await acs5DemoContractStub.SetMethodCallingThreshold.SendAsync(
       new SetMethodCallingThresholdInput
       {
           Method = nameof(acs5DemoContractStub.Foo),
           SymbolToAmount =
           {
               {"ELF", 1_0000_0000}
           },
           ThresholdCheckType = ThresholdCheckType.Balance
       });

Check the threshold again:

.. code:: c#

   methodResult = await acs5DemoContractStub.GetMethodCallingThreshold.CallAsync(
       new StringValue
       {
           Value = nameof(acs5DemoContractStub.Foo)
       });
   methodResult.SymbolToAmount.Count.ShouldBe(1);
   methodResult.ThresholdCheckType.ShouldBe(ThresholdCheckType.Balance);

Send the Foo transaction via an account who has sufficient balance can
succeed:

.. code:: c#

   // Call with enough balance.
   {
       var executionResult = await acs5DemoContractStub.Foo.SendAsync(new Empty());
       executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
   }

Send the Foo transaction via another account without ELF fails:

.. code:: c#

   // Call without enough balance.
   {
       var poorStub =
           GetTester<ACS5DemoContractContainer.ACS5DemoContractStub>(DAppContractAddress,
               SampleECKeyPairs.KeyPairs[1]);
       var executionResult = await poorStub.Foo.SendWithExceptionAsync(new Empty());
       executionResult.TransactionResult.Error.ShouldContain("Cannot meet the calling threshold.");
   }

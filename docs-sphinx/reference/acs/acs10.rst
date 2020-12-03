ACS10 - Dividend Pool Standard
==============================

ACS10 is used to construct a dividend pool in the contract.

Interface
---------

To construct a dividend pool, you can implement the following interfaces
optionally:

Methods
~~~~~~~

+-----------------------------+----------------------------------------------------------------+------------------------------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Method Name                 | Request Type                                                   | Response Type                                        | Description                                                                                                                                                   |
+=============================+================================================================+======================================================+===============================================================================================================================================================+
| Donate                      | `acs10.DonateInput <#acs10.DonateInput>`__                     | `google.protobuf.Empty <#google.protobuf.Empty>`__   | Donates tokens from the caller to the treasury. If the tokens are not native tokens in the current chain, they will be first converted to the native token.   |
+-----------------------------+----------------------------------------------------------------+------------------------------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Release                     | `acs10.ReleaseInput <#acs10.ReleaseInput>`__                   | `google.protobuf.Empty <#google.protobuf.Empty>`__   | Release dividend pool according the period number.                                                                                                            |
+-----------------------------+----------------------------------------------------------------+------------------------------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------+
| SetSymbolList               | `acs10.SymbolList <#acs10.SymbolList>`__                       | `google.protobuf.Empty <#google.protobuf.Empty>`__   | Set the token symbols dividend pool supports.                                                                                                                 |
+-----------------------------+----------------------------------------------------------------+------------------------------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------+
| GetSymbolList               | `google.protobuf.Empty <#google.protobuf.Empty>`__             | `acs10.SymbolList <#acs10.SymbolList>`__             | Query the token symbols dividend pool supports.                                                                                                               |
+-----------------------------+----------------------------------------------------------------+------------------------------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------+
| GetUndistributedDividends   | `google.protobuf.Empty <#google.protobuf.Empty>`__             | `acs10.Dividends <#acs10.Dividends>`__               | Query the balance of undistributed tokens whose symbols are included in the symbol list.                                                                      |
+-----------------------------+----------------------------------------------------------------+------------------------------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------+
| GetDividends                | `google.protobuf.Int64Value <#google.protobuf.Int64Value>`__   | `acs10.Dividends <#acs10.Dividends>`__               | Query the dividend information according to the height.                                                                                                       |
+-----------------------------+----------------------------------------------------------------+------------------------------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------+

Types
~~~~~

.. raw:: html

   <div id="acs10.Dividends">

.. raw:: html

   </div>

acs10.Dividends
^^^^^^^^^^^^^^^

+---------+----------------------------------------------------------+------------------------------------+------------+
| Field   | Type                                                     | Description                        | Label      |
+=========+==========================================================+====================================+============+
| value   | `Dividends.ValueEntry <#acs10.Dividends.ValueEntry>`__   | The dividends, symbol -> amount.   | repeated   |
+---------+----------------------------------------------------------+------------------------------------+------------+

.. raw:: html

   <div id="acs10.Dividends.ValueEntry">

.. raw:: html

   </div>

acs10.Dividends.ValueEntry
^^^^^^^^^^^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| key     | `string <#string>`__   |               |         |
+---------+------------------------+---------------+---------+
| value   | `int64 <#int64>`__     |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="acs10.DonateInput">

.. raw:: html

   </div>

acs10.DonateInput
^^^^^^^^^^^^^^^^^

+----------+------------------------+-------------------------------+---------+
| Field    | Type                   | Description                   | Label   |
+==========+========================+===============================+=========+
| symbol   | `string <#string>`__   | The token symbol to donate.   |         |
+----------+------------------------+-------------------------------+---------+
| amount   | `int64 <#int64>`__     | The amount to donate.         |         |
+----------+------------------------+-------------------------------+---------+

.. raw:: html

   <div id="acs10.DonationReceived">

.. raw:: html

   </div>

acs10.DonationReceived
^^^^^^^^^^^^^^^^^^^^^^

+------------------+------------------------------------+---------------------------------+---------+
| Field            | Type                               | Description                     | Label   |
+==================+====================================+=================================+=========+
| from             | `aelf.Address <#aelf.Address>`__   | The address of donors.          |         |
+------------------+------------------------------------+---------------------------------+---------+
| pool\_contract   | `aelf.Address <#aelf.Address>`__   | The address of dividend pool.   |         |
+------------------+------------------------------------+---------------------------------+---------+
| symbol           | `string <#string>`__               | The token symbol Donated.       |         |
+------------------+------------------------------------+---------------------------------+---------+
| amount           | `int64 <#int64>`__                 | The amount Donated.             |         |
+------------------+------------------------------------+---------------------------------+---------+

.. raw:: html

   <div id="acs10.ReleaseInput">

.. raw:: html

   </div>

acs10.ReleaseInput
^^^^^^^^^^^^^^^^^^

+------------------+----------------------+---------------------------------+---------+
| Field            | Type                 | Description                     | Label   |
+==================+======================+=================================+=========+
| period\_number   | `int64 <#int64>`__   | The period number to release.   |         |
+------------------+----------------------+---------------------------------+---------+

.. raw:: html

   <div id="acs10.SymbolList">

.. raw:: html

   </div>

acs10.SymbolList
^^^^^^^^^^^^^^^^

+---------+------------------------+--------------------------+------------+
| Field   | Type                   | Description              | Label      |
+=========+========================+==========================+============+
| value   | `string <#string>`__   | The token symbol list.   | repeated   |
+---------+------------------------+--------------------------+------------+

.. raw:: html

   <div id="aelf.Address">

.. raw:: html

   </div>

aelf.Address
^^^^^^^^^^^^

+---------+----------------------+---------------+---------+
| Field   | Type                 | Description   | Label   |
+=========+======================+===============+=========+
| value   | `bytes <#bytes>`__   |               |         |
+---------+----------------------+---------------+---------+

.. raw:: html

   <div id="aelf.BinaryMerkleTree">

.. raw:: html

   </div>

aelf.BinaryMerkleTree
^^^^^^^^^^^^^^^^^^^^^

+---------------+-------------------------+---------------------------+------------+
| Field         | Type                    | Description               | Label      |
+===============+=========================+===========================+============+
| nodes         | `Hash <#aelf.Hash>`__   | The leaf nodes.           | repeated   |
+---------------+-------------------------+---------------------------+------------+
| root          | `Hash <#aelf.Hash>`__   | The root node hash.       |            |
+---------------+-------------------------+---------------------------+------------+
| leaf\_count   | `int32 <#int32>`__      | The count of leaf node.   |            |
+---------------+-------------------------+---------------------------+------------+

.. raw:: html

   <div id="aelf.Hash">

.. raw:: html

   </div>

aelf.Hash
^^^^^^^^^

+---------+----------------------+---------------+---------+
| Field   | Type                 | Description   | Label   |
+=========+======================+===============+=========+
| value   | `bytes <#bytes>`__   |               |         |
+---------+----------------------+---------------+---------+

.. raw:: html

   <div id="aelf.LogEvent">

.. raw:: html

   </div>

aelf.LogEvent
^^^^^^^^^^^^^

+----------------+-------------------------------+----------------------------------------------+------------+
| Field          | Type                          | Description                                  | Label      |
+================+===============================+==============================================+============+
| address        | `Address <#aelf.Address>`__   | The contract address.                        |            |
+----------------+-------------------------------+----------------------------------------------+------------+
| name           | `string <#string>`__          | The name of the log event.                   |            |
+----------------+-------------------------------+----------------------------------------------+------------+
| indexed        | `bytes <#bytes>`__            | The indexed data, used to calculate bloom.   | repeated   |
+----------------+-------------------------------+----------------------------------------------+------------+
| non\_indexed   | `bytes <#bytes>`__            | The non indexed data.                        |            |
+----------------+-------------------------------+----------------------------------------------+------------+

.. raw:: html

   <div id="aelf.MerklePath">

.. raw:: html

   </div>

aelf.MerklePath
^^^^^^^^^^^^^^^

+-----------------------+---------------------------------------------+--------------------------+------------+
| Field                 | Type                                        | Description              | Label      |
+=======================+=============================================+==========================+============+
| merkle\_path\_nodes   | `MerklePathNode <#aelf.MerklePathNode>`__   | The merkle path nodes.   | repeated   |
+-----------------------+---------------------------------------------+--------------------------+------------+

.. raw:: html

   <div id="aelf.MerklePathNode">

.. raw:: html

   </div>

aelf.MerklePathNode
^^^^^^^^^^^^^^^^^^^

+-------------------------+-------------------------+------------------------------------+---------+
| Field                   | Type                    | Description                        | Label   |
+=========================+=========================+====================================+=========+
| hash                    | `Hash <#aelf.Hash>`__   | The node hash.                     |         |
+-------------------------+-------------------------+------------------------------------+---------+
| is\_left\_child\_node   | `bool <#bool>`__        | Whether it is a left child node.   |         |
+-------------------------+-------------------------+------------------------------------+---------+

.. raw:: html

   <div id="aelf.SInt32Value">

.. raw:: html

   </div>

aelf.SInt32Value
^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| value   | `sint32 <#sint32>`__   |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="aelf.SInt64Value">

.. raw:: html

   </div>

aelf.SInt64Value
^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| value   | `sint64 <#sint64>`__   |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="aelf.ScopedStatePath">

.. raw:: html

   </div>

aelf.ScopedStatePath
^^^^^^^^^^^^^^^^^^^^

+-----------+-----------------------------------+----------------------------------------------------------+---------+
| Field     | Type                              | Description                                              | Label   |
+===========+===================================+==========================================================+=========+
| address   | `Address <#aelf.Address>`__       | The scope address, which will be the contract address.   |         |
+-----------+-----------------------------------+----------------------------------------------------------+---------+
| path      | `StatePath <#aelf.StatePath>`__   | The path of contract state.                              |         |
+-----------+-----------------------------------+----------------------------------------------------------+---------+

.. raw:: html

   <div id="aelf.SmartContractRegistration">

.. raw:: html

   </div>

aelf.SmartContractRegistration
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+------------------------+-------------------------+-----------------------------------------+---------+
| Field                  | Type                    | Description                             | Label   |
+========================+=========================+=========================================+=========+
| category               | `sint32 <#sint32>`__    | The category of contract code(0: C#).   |         |
+------------------------+-------------------------+-----------------------------------------+---------+
| code                   | `bytes <#bytes>`__      | The byte array of the contract code.    |         |
+------------------------+-------------------------+-----------------------------------------+---------+
| code\_hash             | `Hash <#aelf.Hash>`__   | The hash of the contract code.          |         |
+------------------------+-------------------------+-----------------------------------------+---------+
| is\_system\_contract   | `bool <#bool>`__        | Whether it is a system contract.        |         |
+------------------------+-------------------------+-----------------------------------------+---------+
| version                | `int32 <#int32>`__      | The version of the current contract.    |         |
+------------------------+-------------------------+-----------------------------------------+---------+

.. raw:: html

   <div id="aelf.StatePath">

.. raw:: html

   </div>

aelf.StatePath
^^^^^^^^^^^^^^

+---------+------------------------+---------------------------------------+------------+
| Field   | Type                   | Description                           | Label      |
+=========+========================+=======================================+============+
| parts   | `string <#string>`__   | The partial path of the state path.   | repeated   |
+---------+------------------------+---------------------------------------+------------+

.. raw:: html

   <div id="aelf.Transaction">

.. raw:: html

   </div>

aelf.Transaction
^^^^^^^^^^^^^^^^

+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| Field                | Type                          | Description                                                                                                                                                                                        | Label   |
+======================+===============================+====================================================================================================================================================================================================+=========+
| from                 | `Address <#aelf.Address>`__   | The address of the sender of the transaction.                                                                                                                                                      |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| to                   | `Address <#aelf.Address>`__   | The address of the contract when calling a contract.                                                                                                                                               |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| ref\_block\_number   | `int64 <#int64>`__            | The height of the referenced block hash.                                                                                                                                                           |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| ref\_block\_prefix   | `bytes <#bytes>`__            | The first four bytes of the referenced block hash.                                                                                                                                                 |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| method\_name         | `string <#string>`__          | The name of a method in the smart contract at the To address.                                                                                                                                      |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| params               | `bytes <#bytes>`__            | The parameters to pass to the smart contract method.                                                                                                                                               |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| signature            | `bytes <#bytes>`__            | When signing a transaction it’s actually a subset of the fields: from/to and the target method as well as the parameter that were given. It also contains the reference block number and prefix.   |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+

.. raw:: html

   <div id="aelf.TransactionExecutingStateSet">

.. raw:: html

   </div>

aelf.TransactionExecutingStateSet
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+-----------+---------------------------------------------------------------------------------------------------+-----------------------+------------+
| Field     | Type                                                                                              | Description           | Label      |
+===========+===================================================================================================+=======================+============+
| writes    | `TransactionExecutingStateSet.WritesEntry <#aelf.TransactionExecutingStateSet.WritesEntry>`__     | The changed states.   | repeated   |
+-----------+---------------------------------------------------------------------------------------------------+-----------------------+------------+
| reads     | `TransactionExecutingStateSet.ReadsEntry <#aelf.TransactionExecutingStateSet.ReadsEntry>`__       | The read states.      | repeated   |
+-----------+---------------------------------------------------------------------------------------------------+-----------------------+------------+
| deletes   | `TransactionExecutingStateSet.DeletesEntry <#aelf.TransactionExecutingStateSet.DeletesEntry>`__   | The deleted states.   | repeated   |
+-----------+---------------------------------------------------------------------------------------------------+-----------------------+------------+

.. raw:: html

   <div id="aelf.TransactionExecutingStateSet.DeletesEntry">

.. raw:: html

   </div>

aelf.TransactionExecutingStateSet.DeletesEntry
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| key     | `string <#string>`__   |               |         |
+---------+------------------------+---------------+---------+
| value   | `bool <#bool>`__       |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="aelf.TransactionExecutingStateSet.ReadsEntry">

.. raw:: html

   </div>

aelf.TransactionExecutingStateSet.ReadsEntry
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| key     | `string <#string>`__   |               |         |
+---------+------------------------+---------------+---------+
| value   | `bool <#bool>`__       |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="aelf.TransactionExecutingStateSet.WritesEntry">

.. raw:: html

   </div>

aelf.TransactionExecutingStateSet.WritesEntry
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| key     | `string <#string>`__   |               |         |
+---------+------------------------+---------------+---------+
| value   | `bytes <#bytes>`__     |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="aelf.TransactionResult">

.. raw:: html

   </div>

aelf.TransactionResult
^^^^^^^^^^^^^^^^^^^^^^

+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| Field             | Type                                                          | Description                                                                                                                                                                                                                                                                | Label      |
+===================+===============================================================+============================================================================================================================================================================================================================================================================+============+
| transaction\_id   | `Hash <#aelf.Hash>`__                                         | The transaction id.                                                                                                                                                                                                                                                        |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| status            | `TransactionResultStatus <#aelf.TransactionResultStatus>`__   | The transaction result status.                                                                                                                                                                                                                                             |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| logs              | `LogEvent <#aelf.LogEvent>`__                                 | The log events.                                                                                                                                                                                                                                                            | repeated   |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| bloom             | `bytes <#bytes>`__                                            | Bloom filter for transaction logs. A transaction log event can be defined in the contract and stored in the bloom filter after the transaction is executed. Through this filter, we can quickly search for and determine whether a log exists in the transaction result.   |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| return\_value     | `bytes <#bytes>`__                                            | The return value of the transaction execution.                                                                                                                                                                                                                             |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| block\_number     | `int64 <#int64>`__                                            | The height of the block hat packages the transaction.                                                                                                                                                                                                                      |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| block\_hash       | `Hash <#aelf.Hash>`__                                         | The hash of the block hat packages the transaction.                                                                                                                                                                                                                        |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| error             | `string <#string>`__                                          | Failed execution error message.                                                                                                                                                                                                                                            |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+

.. raw:: html

   <div id="aelf.TransactionResultStatus">

.. raw:: html

   </div>

aelf.TransactionResultStatus
^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+----------------------------+----------+-------------------------------------------------------------------------------------+
| Name                       | Number   | Description                                                                         |
+============================+==========+=====================================================================================+
| NOT\_EXISTED               | 0        | The execution result of the transaction does not exist.                             |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| PENDING                    | 1        | The transaction is in the transaction pool waiting to be packaged.                  |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| FAILED                     | 2        | Transaction execution failed.                                                       |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| MINED                      | 3        | The transaction was successfully executed and successfully packaged into a block.   |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| CONFLICT                   | 4        | When executed in parallel, there are conflicts with other transactions.             |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| PENDING\_VALIDATION        | 5        | The transaction is waiting for validation.                                          |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| NODE\_VALIDATION\_FAILED   | 6        | Transaction validation failed.                                                      |
+----------------------------+----------+-------------------------------------------------------------------------------------+

Usage
-----

ACS10 only unifies the standard interface of the dividend pool, which
does not interact with the AElf chain.

Implementation
--------------

With the Profit contract
~~~~~~~~~~~~~~~~~~~~~~~~

A Profit Scheme can be created using the ``CreateScheme`` method of
``Profit contract``:

.. code:: c#

   State.ProfitContract.Value =
       Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
   var schemeToken = HashHelper.ComputeFrom(Context.Self);
   State.ProfitContract.CreateScheme.Send(new CreateSchemeInput
   {
       Manager = Context.Self,
       CanRemoveBeneficiaryDirectly = true,
       IsReleaseAllBalanceEveryTimeByDefault = true,
       Token = schemeToken
   });
   State.ProfitSchemeId.Value = Context.GenerateId(State.ProfitContract.Value, schemeToken);

The Context.GenerateId method is a common method used by the AElf to
generate Id. We use the address of the Profit contract and the
schemeToken provided to the Profit contract to calculate the Id of the
scheme, and we set this id to State.ProfitSchemeId
(SingletonState<Hash>).

After the establishment of the dividend scheme:

-  ``ContributeProfits`` method of Profit can be used to implement the
   method Donate in ACS10.
-  The Release in the ACS10 can be implemented using the method
   ``DistributeProfits`` in the ``Profit contract``;
-  Methods such as ``AddBeneficiary`` and ``RemoveBeneficiary`` can be
   used to manage the recipients and their weight.
-  ``AddSubScheme``, ``RemoveSubScheme`` and other methods can be used
   to manage the sub-dividend scheme and its weight;
-  The ``SetSymbolList`` and ``GetSymbolList`` can be implemented by
   yourself. Just make sure the symbol list you set is used correctly in
   ``Donate`` and ``Release``.
-  ``GetUndistributedDividends`` returns the balance of the token whose
   symbol is included in symbol list.

With TokenHolder Contract
~~~~~~~~~~~~~~~~~~~~~~~~~

When initializing the contract, you should create a token holder
dividend scheme using the CreateScheme in the TokenHolder contract(Token
Holder Profit Scheme）：

.. code:: c#

   State.TokenHolderContract.Value =
       Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName);
   State.TokenHolderContract.CreateScheme.Send(new CreateTokenHolderProfitSchemeInput
   {
       Symbol = Context.Variables.NativeSymbol,
       MinimumLockMinutes = input.MinimumLockMinutes
   });
   return new Empty();

In a token holder dividend scheme, a scheme is bound to its creator, so
SchemeId is not necessary to compute (in fact, the scheme is created via
the Profit contract).

Considering the ``GetDividends`` returns the dividend information
according to the input height, so each Donate need update dividend
information for each height . A Donate can be implemented as:

.. code:: c#

   public override Empty Donate(DonateInput input)
   {
       State.TokenContract.TransferFrom.Send(new TransferFromInput
       {
           From = Context.Sender,
           Symbol = input.Symbol,
           Amount = input.Amount,
           To = Context.Self
       });
       State.TokenContract.Approve.Send(new ApproveInput
       {
           Symbol = input.Symbol,
           Amount = input.Amount,
           Spender = State.TokenHolderContract.Value
       });
       State.TokenHolderContract.ContributeProfits.Send(new ContributeProfitsInput
       {
           SchemeManager = Context.Self,
           Symbol = input.Symbol,
           Amount = input.Amount
       });
       Context.Fire(new DonationReceived
       {
           From = Context.Sender,
           Symbol = input.Symbol,
           Amount = input.Amount,
           PoolContract = Context.Self
       });
       var currentReceivedDividends = State.ReceivedDividends[Context.CurrentHeight];
       if (currentReceivedDividends != null && currentReceivedDividends.Value.ContainsKey(input.Symbol))
       {
           currentReceivedDividends.Value[input.Symbol] =
               currentReceivedDividends.Value[input.Symbol].Add(input.Amount);
       }
       else
       {
           currentReceivedDividends = new Dividends
           {
               Value =
               {
                   {
                       input.Symbol, input.Amount
                   }
               }
           };
       }
       State.ReceivedDividends[Context.CurrentHeight] = currentReceivedDividends;
       Context.LogDebug(() => string.Format("Contributed {0} {1}s to side chain dividends pool.", input.Amount, input.Symbol));
       return new Empty();
   }

The method Release directly sends the TokenHolder’s method
``DistributeProfits`` transaction:

.. code:: c#

   public override Empty Release(ReleaseInput input)
   {
       State.TokenHolderContract.DistributeProfits.Send(new DistributeProfitsInput
       {
           SchemeManager = Context.Self
       });
       return new Empty();
   }

In the ``TokenHolder contract``, the default implementation is to
release what token is received, so ``SetSymbolList`` does not need to be
implemented, and ``GetSymbolList`` returns the symbol list recorded in
dividend scheme:

.. code:: c#

   public override Empty SetSymbolList(SymbolList input)
   {
       Assert(false, "Not support setting symbol list.");
       return new Empty();
   }
   public override SymbolList GetSymbolList(Empty input)
   {
       return new SymbolList
       {
           Value =
           {
               GetDividendPoolScheme().ReceivedTokenSymbols
           }
       };
   }
   private Scheme GetDividendPoolScheme()
   {
       if (State.DividendPoolSchemeId.Value == null)
       {
           var tokenHolderScheme = State.TokenHolderContract.GetScheme.Call(Context.Self);
           State.DividendPoolSchemeId.Value = tokenHolderScheme.SchemeId;
       }
       return Context.Call<Scheme>(
           Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName),
           nameof(ProfitContractContainer.ProfitContractReferenceState.GetScheme),
           State.DividendPoolSchemeId.Value);
   }

The implementation of ``GetUndistributedDividends`` is the same as
described in the previous section, and it returns the balance:

.. code:: c#

   public override Dividends GetUndistributedDividends(Empty input)
   {
       var scheme = GetDividendPoolScheme();
       return new Dividends
       {
           Value =
           {
               scheme.ReceivedTokenSymbols.Select(s => State.TokenContract.GetBalance.Call(new GetBalanceInput
               {
                   Owner = scheme.VirtualAddress,
                   Symbol = s
               })).ToDictionary(b => b.Symbol, b => b.Balance)
           }
       };
   }

In addition to the ``Profit`` and ``TokenHolder`` contracts, of course,
you can also implement a dividend pool on your own contract.

Test
----

The dividend pool, for example, is tested in two ways with the
``TokenHolder contract``.

One way is for the dividend pool to send Donate, Release and a series of
query operations;

The other way is to use an account to lock up, and then take out
dividends.

Define the required Stubs:

.. code:: c#

   const long amount = 10_00000000;
   var keyPair = SampleECKeyPairs.KeyPairs[0];
   var address = Address.FromPublicKey(keyPair.PublicKey);
   var acs10DemoContractStub =
       GetTester<ACS10DemoContractContainer.ACS10DemoContractStub>(DAppContractAddress, keyPair);
   var tokenContractStub =
       GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
   var tokenHolderContractStub =
       GetTester<TokenHolderContractContainer.TokenHolderContractStub>(TokenHolderContractAddress,
           keyPair);

Before proceeding, You should Approve the ``TokenHolder contract`` and
the dividend pool contract.

.. code:: c#

   await tokenContractStub.Approve.SendAsync(new ApproveInput
   {
       Spender = TokenHolderContractAddress,
       Symbol = "ELF",
       Amount = long.MaxValue
   });
   await tokenContractStub.Approve.SendAsync(new ApproveInput
   {
       Spender = DAppContractAddress,
       Symbol = "ELF",
       Amount = long.MaxValue
   });

Lock the position, at which point the account balance is reduced by 10
ELF:

.. code:: c#

   await tokenHolderContractStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
   {
       SchemeManager = DAppContractAddress,
       Amount = amount
   });

Donate, at which point the account balance is reduced by another 10 ELF:

.. code:: c#

   await acs10DemoContractStub.Donate.SendAsync(new DonateInput
   {
       Symbol = "ELF",
       Amount = amount
   });

At this point you can test the ``GetUndistributedDividends`` and
``GetDividends``:

.. code:: c#

   // Check undistributed dividends before releasing.
   {
       var undistributedDividends =
           await acs10DemoContractStub.GetUndistributedDividends.CallAsync(new Empty());
       undistributedDividends.Value["ELF"].ShouldBe(amount);
   }
   var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
   var currentBlockHeight = (await blockchainService.GetChainAsync()).BestChainHeight;
   var dividends =
       await acs10DemoContractStub.GetDividends.CallAsync(new Int64Value {Value = currentBlockHeight});
   dividends.Value["ELF"].ShouldBe(amount);

Release bonus, and test ``GetUndistributedDividends`` again:

.. code:: c#

   await acs10DemoContractStub.Release.SendAsync(new ReleaseInput
   {
       PeriodNumber = 1
   });
   // Check undistributed dividends after releasing.
   {
       var undistributedDividends =
           await acs10DemoContractStub.GetUndistributedDividends.CallAsync(new Empty());
       undistributedDividends.Value["ELF"].ShouldBe(0);
   }

Finally, let this account receive the dividend and then observe the
change in its balance:

.. code:: c#

   var balanceBeforeClaimForProfits = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput
   {
       Owner = address,
       Symbol = "ELF"
   });
   await tokenHolderContractStub.ClaimProfits.SendAsync(new ClaimProfitsInput
   {
       SchemeManager = DAppContractAddress,
       Beneficiary = address
   });
   var balanceAfterClaimForProfits = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput
   {
       Owner = address,
       Symbol = "ELF"
   });
   balanceAfterClaimForProfits.Balance.ShouldBe(balanceBeforeClaimForProfits.Balance + amount);

Example
-------

The dividend pool of the main chain and the side chain is built by
implementing ACS10.

The dividend pool provided by the ``Treasury contract`` implementing
ACS10 is on the main chain.

The dividend pool provided by the ``Consensus contract`` implementing
ACS10 is on the side chain.

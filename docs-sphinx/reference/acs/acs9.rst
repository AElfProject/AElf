ACS9 - Contract profit dividend standard
========================================

On the AElf’s side chain, the contract needs to declare where its
profits are going, and implement ACS9.

Interface
---------

ACS9 contains an method which does not have to be implemented:

Methods
~~~~~~~

+-----------------------+----------------------------------------------------------------------+------------------------------------------------------+-------------------------------------------------------------------------------------------------------------------------+
| Method Name           | Request Type                                                         | Response Type                                        | Description                                                                                                             |
+=======================+======================================================================+======================================================+=========================================================================================================================+
| TakeContractProfits   | `acs9.TakeContractProfitsInput <#acs9.TakeContractProfitsInput>`__   | `google.protobuf.Empty <#google.protobuf.Empty>`__   | Used for the developer to collect the profits from the contract， and the profits will be distributed in this method.   |
+-----------------------+----------------------------------------------------------------------+------------------------------------------------------+-------------------------------------------------------------------------------------------------------------------------+
| GetProfitConfig       | `google.protobuf.Empty <#google.protobuf.Empty>`__                   | `acs9.ProfitConfig <#acs9.ProfitConfig>`__           | Query the config of profit.                                                                                             |
+-----------------------+----------------------------------------------------------------------+------------------------------------------------------+-------------------------------------------------------------------------------------------------------------------------+
| GetProfitsAmount      | `google.protobuf.Empty <#google.protobuf.Empty>`__                   | `acs9.ProfitsMap <#acs9.ProfitsMap>`__               | Query the profits of the contract so far.                                                                               |
+-----------------------+----------------------------------------------------------------------+------------------------------------------------------+-------------------------------------------------------------------------------------------------------------------------+

Types
~~~~~

.. raw:: html

   <div id="acs9.ProfitConfig">

.. raw:: html

   </div>

acs9.ProfitConfig
^^^^^^^^^^^^^^^^^

+---------------------------------+------------------------+--------------------------------------------------------------------------------------------------------------------+------------+
| Field                           | Type                   | Description                                                                                                        | Label      |
+=================================+========================+====================================================================================================================+============+
| donation\_parts\_per\_hundred   | `int32 <#int32>`__     | The portion of the profit that will be donated to the dividend pool each time the developer receives the profit.   |            |
+---------------------------------+------------------------+--------------------------------------------------------------------------------------------------------------------+------------+
| profits\_token\_symbol\_list    | `string <#string>`__   | The profit token symbol list.                                                                                      | repeated   |
+---------------------------------+------------------------+--------------------------------------------------------------------------------------------------------------------+------------+
| staking\_token\_symbol          | `string <#string>`__   | The token symbol that the user can lock them to claim the profit.                                                  |            |
+---------------------------------+------------------------+--------------------------------------------------------------------------------------------------------------------+------------+

.. raw:: html

   <div id="acs9.ProfitsMap">

.. raw:: html

   </div>

acs9.ProfitsMap
^^^^^^^^^^^^^^^

+---------+-----------------------------------------------------------+----------------------------------------+------------+
| Field   | Type                                                      | Description                            | Label      |
+=========+===========================================================+========================================+============+
| value   | `ProfitsMap.ValueEntry <#acs9.ProfitsMap.ValueEntry>`__   | The profits, token symbol -> amount.   | repeated   |
+---------+-----------------------------------------------------------+----------------------------------------+------------+

.. raw:: html

   <div id="acs9.ProfitsMap.ValueEntry">

.. raw:: html

   </div>

acs9.ProfitsMap.ValueEntry
^^^^^^^^^^^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| key     | `string <#string>`__   |               |         |
+---------+------------------------+---------------+---------+
| value   | `int64 <#int64>`__     |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="acs9.TakeContractProfitsInput">

.. raw:: html

   </div>

acs9.TakeContractProfitsInput
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+----------+------------------------+-----------------------------+---------+
| Field    | Type                   | Description                 | Label   |
+==========+========================+=============================+=========+
| symbol   | `string <#string>`__   | The token symbol to take.   |         |
+----------+------------------------+-----------------------------+---------+
| amount   | `int64 <#int64>`__     | The amount to take.         |         |
+----------+------------------------+-----------------------------+---------+

Implementation
--------------

Here we define a contract. The contract creates a token called APP at
the time of initialization and uses the ``TokenHolder`` contract to
create a token holder bonus scheme with the lock token is designated to
APP.

The user will be given 10 APP when to sign up.

Users can purchase 1 APP with 1 ELF using method Deposit, and they can
redeem the ELF using the method Withdraw.

When the user sends the Use transaction, the APP token is consumed.

Contract initialization is as follows:

.. code:: c#

   public override Empty Initialize(InitializeInput input)
   {
       State.TokenHolderContract.Value =
           Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName);
       State.TokenContract.Value =
           Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
       State.DividendPoolContract.Value =
           Context.GetContractAddressByName(input.DividendPoolContractName.Value.ToBase64());
       State.Symbol.Value = input.Symbol == string.Empty ? "APP" : input.Symbol;
       State.ProfitReceiver.Value = input.ProfitReceiver;
       CreateToken(input.ProfitReceiver);
       // To test TokenHolder Contract.
       CreateTokenHolderProfitScheme();
       // To test ACS9 workflow.
       SetProfitConfig();
       State.ProfitReceiver.Value = input.ProfitReceiver;
       return new Empty();
   }
   private void CreateToken(Address profitReceiver, bool isLockWhiteListIncludingSelf = false)
   {
       var lockWhiteList = new List<Address>
           {Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName)};
       if (isLockWhiteListIncludingSelf)
           lockWhiteList.Add(Context.Self);
       State.TokenContract.Create.Send(new CreateInput
       {
           Symbol = State.Symbol.Value,
           TokenName = "DApp Token",
           Decimals = ACS9DemoContractConstants.Decimal,
           Issuer = Context.Self,
           IsBurnable = true,
           IsProfitable = true,
           TotalSupply = ACS9DemoContractConstants.TotalSupply,
           LockWhiteList =
           {
               lockWhiteList
           }
       });
       State.TokenContract.Issue.Send(new IssueInput
       {
           To = profitReceiver,
           Amount = ACS9DemoContractConstants.TotalSupply / 5,
           Symbol = State.Symbol.Value,
           Memo = "Issue token for profit receiver"
       });
   }
   private void CreateTokenHolderProfitScheme()
   {
       State.TokenHolderContract.CreateScheme.Send(new CreateTokenHolderProfitSchemeInput
       {
           Symbol = State.Symbol.Value
       });
   }
   private void SetProfitConfig()
   {
       State.ProfitConfig.Value = new ProfitConfig
       {
           DonationPartsPerHundred = 1,
           StakingTokenSymbol = "APP",
           ProfitsTokenSymbolList = {"ELF"}
       };
   }

The State.symbol is a singleton of type string, state.Profitconfig is a
singleton of type ``ProfitConfig``, and state.profitreceiver is a
singleton of type ``Address``.

The user can use the SighUp method to register and get the bonus.
Besides, it will create a archive for him:

.. code:: c#

   /// <summary>
   /// When user sign up, give him 10 APP tokens, then initialize his profile.
   /// </summary>
   /// <param name="input"></param>
   /// <returns></returns>
   public override Empty SignUp(Empty input)
   {
       Assert(State.Profiles[Context.Sender] == null, "Already registered.");
       var profile = new Profile
       {
           UserAddress = Context.Sender
       };
       State.TokenContract.Issue.Send(new IssueInput
       {
           Symbol = State.Symbol.Value,
           Amount = ACS9DemoContractConstants.ForNewUser,
           To = Context.Sender
       });
       // Update profile.
       profile.Records.Add(new Record
       {
           Type = RecordType.SignUp,
           Timestamp = Context.CurrentBlockTime,
           Description = string.Format("{0} +{1}",State.Symbol.Value, ACS9DemoContractConstants.ForNewUser)
       });
       State.Profiles[Context.Sender] = profile;
       return new Empty();
   }

Recharge and redemption:

.. code:: c#

   public override Empty Deposit(DepositInput input)
   {
       // User Address -> DApp Contract.
       State.TokenContract.TransferFrom.Send(new TransferFromInput
       {
           From = Context.Sender,
           To = Context.Self,
           Symbol = "ELF",
           Amount = input.Amount
       });
       State.TokenContract.Issue.Send(new IssueInput
       {
           Symbol = State.Symbol.Value,
           Amount = input.Amount,
           To = Context.Sender
       });
       // Update profile.
       var profile = State.Profiles[Context.Sender];
       profile.Records.Add(new Record
       {
           Type = RecordType.Deposit,
           Timestamp = Context.CurrentBlockTime,
           Description = string.Format("{0} +{1}", State.Symbol.Value, input.Amount)
       });
       State.Profiles[Context.Sender] = profile;
       return new Empty();
   }
   public override Empty Withdraw(WithdrawInput input)
   {
       State.TokenContract.TransferFrom.Send(new TransferFromInput
       {
           From = Context.Sender,
           To = Context.Self,
           Symbol = State.Symbol.Value,
           Amount = input.Amount
       });
       State.TokenContract.Transfer.Send(new TransferInput
       {
           To = Context.Sender,
           Symbol = input.Symbol,
           Amount = input.Amount
       });
       State.TokenHolderContract.RemoveBeneficiary.Send(new RemoveTokenHolderBeneficiaryInput
       {
           Beneficiary = Context.Sender,
           Amount = input.Amount
       });
       // Update profile.
       var profile = State.Profiles[Context.Sender];
       profile.Records.Add(new Record
       {
           Type = RecordType.Withdraw,
           Timestamp = Context.CurrentBlockTime,
           Description = string.Format("{0} -{1}", State.Symbol.Value, input.Amount)
       });
       State.Profiles[Context.Sender] = profile;
       return new Empty();
   }

In the implementation of Use, 1/3 profits are directly transferred into
the token holder dividend scheme:

.. code:: c#

   public override Empty Use(Record input)
   {
       State.TokenContract.TransferFrom.Send(new TransferFromInput
       {
           From = Context.Sender,
           To = Context.Self,
           Symbol = State.Symbol.Value,
           Amount = ACS9DemoContractConstants.UseFee
       });
       if (input.Symbol == string.Empty)
           input.Symbol = State.TokenContract.GetPrimaryTokenSymbol.Call(new Empty()).Value;
       var contributeAmount = ACS9DemoContractConstants.UseFee.Div(3);
       State.TokenContract.Approve.Send(new ApproveInput
       {
           Spender = State.TokenHolderContract.Value,
           Symbol = input.Symbol,
           Amount = contributeAmount
       });
       // Contribute 1/3 profits (ELF) to profit scheme.
       State.TokenHolderContract.ContributeProfits.Send(new ContributeProfitsInput
       {
           SchemeManager = Context.Self,
           Amount = contributeAmount,
           Symbol = input.Symbol
       });
       // Update profile.
       var profile = State.Profiles[Context.Sender];
       profile.Records.Add(new Record
       {
           Type = RecordType.Withdraw,
           Timestamp = Context.CurrentBlockTime,
           Description = string.Format("{0} -{1}", State.Symbol.Value, ACS9DemoContractConstants.UseFee),
           Symbol = input.Symbol
       });
       State.Profiles[Context.Sender] = profile;
       return new Empty();
   }

The implementation of this contract has been completed. Next, implement
ACS9 to perfect the profit distribution:

.. code:: c#

   public override Empty TakeContractProfits(TakeContractProfitsInput input)
   {
       var config = State.ProfitConfig.Value;
       // For Side Chain Dividends Pool.
       var amountForSideChainDividendsPool = input.Amount.Mul(config.DonationPartsPerHundred).Div(100);
       State.TokenContract.Approve.Send(new ApproveInput
       {
           Symbol = input.Symbol,
           Amount = amountForSideChainDividendsPool,
           Spender = State.DividendPoolContract.Value
       });
       State.DividendPoolContract.Donate.Send(new DonateInput
       {
           Symbol = input.Symbol,
           Amount = amountForSideChainDividendsPool
       });
       // For receiver.
       var amountForReceiver = input.Amount.Sub(amountForSideChainDividendsPool);
       State.TokenContract.Transfer.Send(new TransferInput
       {
           To = State.ProfitReceiver.Value,
           Amount = amountForReceiver,
           Symbol = input.Symbol
       });
       // For Token Holder Profit Scheme. (To distribute.)
       State.TokenHolderContract.DistributeProfits.Send(new DistributeProfitsInput
       {
           SchemeManager = Context.Self
       });
       return new Empty();
   }
   public override ProfitConfig GetProfitConfig(Empty input)
   {
       return State.ProfitConfig.Value;
   }
   public override ProfitsMap GetProfitsAmount(Empty input)
   {
       var profitsMap = new ProfitsMap();
       foreach (var symbol in State.ProfitConfig.Value.ProfitsTokenSymbolList)
       {
           var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
           {
               Owner = Context.Self,
               Symbol = symbol
           }).Balance;
           profitsMap.Value[symbol] = balance;
       }
       return profitsMap;
   }

Test
----

Since part of the profits from the ACS9 contract transfer to the
``Token contract`` and the other transfer to the dividend pool, a
``TokenHolder`` Stub and a contract implementing ACS10 Stub are required
in the test. Accordingly, the contracts that implements ACS9 or ACS10
need to be deployed. Before the test begins, the contract implementing
ACS9 can be initialized by interface
``IContractInitializationProvider``, and sets the dividend pool’s name
to the other contract’s name:

.. code:: c#

   public class ACS9DemoContractInitializationProvider : IContractInitializationProvider
   {
       public List<InitializeMethod> GetInitializeMethodList(byte[] contractCode)
       {
           return new List<InitializeMethod>
           {
               new InitializeMethod
               {
                   MethodName = nameof(ACS9DemoContract.Initialize),
                   Params = new InitializeInput
                   {
                       ProfitReceiver = Address.FromPublicKey(SampleECKeyPairs.KeyPairs.Skip(3).First().PublicKey),
                       DividendPoolContractName = ACS10DemoSmartContractNameProvider.Name
                   }.ToByteString()
               }
           };
       }
       public Hash SystemSmartContractName { get; } = ACS9DemoSmartContractNameProvider.Name;
       public string ContractCodeName { get; } = "AElf.Contracts.ACS9DemoContract";
   }

Prepare a user account:

.. code:: c#

   protected List<ECKeyPair> UserKeyPairs => SampleECKeyPairs.KeyPairs.Skip(2).Take(3).ToList();

Prepare some Stubs:

.. code:: c#

   var keyPair = UserKeyPairs[0];
   var address = Address.FromPublicKey(keyPair.PublicKey);
   // Prepare stubs.
   var acs9DemoContractStub = GetACS9DemoContractStub(keyPair);
   var acs10DemoContractStub = GetACS10DemoContractStub(keyPair);
   var userTokenStub =
       GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, UserKeyPairs[0]);
   var userTokenHolderStub =
       GetTester<TokenHolderContractContainer.TokenHolderContractStub>(TokenHolderContractAddress,
           UserKeyPairs[0]);

Then, transfer ELF to the user (TokenContractStub is the Stub of the
initial bp who has much ELF) :

.. code:: c#

   // Transfer some ELFs to user.
   await TokenContractStub.Transfer.SendAsync(new TransferInput
   {
       To = address,
       Symbol = "ELF",
       Amount = 1000_00000000
   });

Have the user call SignUp to check if he/she has got 10 APP tokens:

.. code:: c#

   await acs9DemoContractStub.SignUp.SendAsync(new Empty());
   // User has 10 APP tokens because of signing up.
   (await GetFirstUserBalance("APP")).ShouldBe(10_00000000);

Test the recharge method of the contract itself:

.. code:: c#

   var elfBalanceBefore = await GetFirstUserBalance("ELF");
   // User has to Approve an amount of ELF tokens before deposit to the DApp.
   await userTokenStub.Approve.SendAsync(new ApproveInput
   {
       Amount = 1000_00000000,
       Spender = ACS9DemoContractAddress,
       Symbol = "ELF"
   });
   await acs9DemoContractStub.Deposit.SendAsync(new DepositInput
   {
       Amount = 100_00000000
   });
   // Check the change of balance of ELF.
   var elfBalanceAfter = await GetFirstUserBalance("ELF");
   elfBalanceAfter.ShouldBe(elfBalanceBefore - 100_00000000);
   // Now user has 110 APP tokens.
   (await GetFirstUserBalance("APP")).ShouldBe(110_00000000);

The user locks up 57 APP via the ``TokenHolder contract`` in order to
obtain profits from the contract:

.. code:: c#

   // User lock some APP tokens for getting profits. (APP -57)
   await userTokenHolderStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
   {
       SchemeManager = ACS9DemoContractAddress,
       Amount = 57_00000000
   });

The Use method is invoked 10 times and 0.3 APP is consumed each time,
and finally the user have 50 APP left:

.. code:: c#

   await userTokenStub.Approve.SendAsync(new ApproveInput
   {
       Amount = long.MaxValue,
       Spender = ACS9DemoContractAddress,
       Symbol = "APP"
   });
   // User uses 10 times of this DApp. (APP -3)
   for (var i = 0; i < 10; i++)
   {
       await acs9DemoContractStub.Use.SendAsync(new Record());
   }
   // Now user has 50 APP tokens.
   (await GetFirstUserBalance("APP")).ShouldBe(50_00000000);

Using the ``TakeContractProfits`` method, the developer attempts to
withdraw 10 ELF as profits. The 10 ELF will be transferred to the
developer in this method:

.. code:: c#

   const long baseBalance = 0;
   {
       var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
       {
           Owner = UserAddresses[1], Symbol = "ELF"
       });
       balance.Balance.ShouldBe(baseBalance);
   }
   // Profits receiver claim 10 ELF profits.
   await acs9DemoContractStub.TakeContractProfits.SendAsync(new TakeContractProfitsInput
   {
       Symbol = "ELF",
       Amount = 10_0000_0000
   });
   // Then profits receiver should have 9.9 ELF tokens.
   {
       var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
       {
           Owner = UserAddresses[1], Symbol = "ELF"
       });
       balance.Balance.ShouldBe(baseBalance + 9_9000_0000);
   }

Next check the profit distribution results. The dividend pool should be
allocated 0.1 ELF:

.. code:: c#

   // And Side Chain Dividends Pool should have 0.1 ELF tokens.
   {
       var scheme = await TokenHolderContractStub.GetScheme.CallAsync(ACS10DemoContractAddress);
       var virtualAddress = await ProfitContractStub.GetSchemeAddress.CallAsync(new SchemePeriod
       {
           SchemeId = scheme.SchemeId,
           Period = 0
       });
       var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
       {
           Owner = virtualAddress,
           Symbol = "ELF"
       });
       balance.Balance.ShouldBe(1000_0000);
   }

The user receives 1 ELF from the token holder dividend scheme:

.. code:: c#

   // Help user to claim profits from token holder profit scheme.
   await TokenHolderContractStub.ClaimProfits.SendAsync(new ClaimProfitsInput
   {
       Beneficiary = UserAddresses[0],
       SchemeManager = ACS9DemoContractAddress,
   });
   // Profits should be 1 ELF.
   (await GetFirstUserBalance("ELF")).ShouldBe(elfBalanceAfter + 1_0000_0000);

Finally, let’s test the Withdraw method.

.. code:: c#

   // Withdraw
   var beforeBalance =
       await userTokenStub.GetBalance.CallAsync(new GetBalanceInput
       {
           Symbol = "APP",
           Owner = UserAddresses[0]
       });
   var withDrawResult = await userTokenHolderStub.Withdraw.SendAsync(ACS9DemoContractAddress);
   withDrawResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
   var resultBalance = await userTokenStub.GetBalance.CallAsync(new GetBalanceInput
   {
       Symbol = "APP",
       Owner = UserAddresses[0]
   });
   resultBalance.Balance.ShouldBe(beforeBalance.Balance + 57_00000000);

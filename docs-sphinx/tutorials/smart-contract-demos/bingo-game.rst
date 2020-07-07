Bingo Game
==========

Requirement Analysis
--------------------

Basic Requirement
~~~~~~~~~~~~~~~~~

Only one rule：Users can bet a certain amount of ELF on Bingo contract,
and then users will gain more ELF or to lose all ELF bet before in the
expected time.

For users, operation steps are as follows:

1. Send an Approve transaction by Token Contract to grant Bingo Contract
   amount of ELF.
2. Bet by Bingo Contract, and the outcome will be unveiled in the
   expected time.
3. After a certain time, or after the block height is reached, the user
   can use the Bingo contract to query the results, and at the same
   time, the Bingo contract will transfer a certain amount of ELF to the
   user (If the amount at this time is greater than the bet amount, it
   means that the user won; vice versa).

API List
--------

In summary, two basic APIs are needed:

1. Play, corresponding to step 2;
2. Bingo, corresponding to step 3.

In order to make the Bingo contract a more complete DApp contract, two
additional Action methods are added:

1. Register, which creates a file for users, can save the registration
   time and user’s eigenvalues (these eigenvalues participate in the
   calculation of the random number used in the Bingo game);
2. Quit, which deletes users’ file.

In addition, there are some View methods for querying information only:

1. GetAward, which allows users to query the award information of a bet;
2. GetPlayerInformation, used to query player’s information. 

+-------------------------+-----------------+-----------------+----------------------------+
| Method                  | Parameters      | Return          | function                   |
+=========================+=================+=================+============================+
| ``Register``            | Empty           | Empty           | register player information|
+-------------------------+-----------------+-----------------+----------------------------+
| ``Quit``                | Empty           | Empty           | delete player information  |
+-------------------------+-----------------+-----------------+----------------------------+
| ``Play``                | Int64Value      | Int64Value      | debt                       |
|                         |                 |                 |                            |
|                         | anount you debt | the resulting   |                            |
|                         |                 |                 |                            |
|                         |                 | block height    |                            |
+-------------------------+-----------------+-----------------+----------------------------+
| ``Bingo``               | Hash            | Empty           | query the game's result    |
|                         |                 |                 |                            |
|                         | the transaction | True indicates  |                            |
|                         |                 |                 |                            |
|                         | id of Play      | win             |                            |
+-------------------------+-----------------+-----------------+----------------------------+
| ``GetAward``            | Hash            | Int64Value      | query the amount of award  |
|                         |                 |                 |                            |
|                         | the transaction | award           |                            |
|                         |                 |                 |                            |
|                         | id of Play      |                 |                            |
+-------------------------+-----------------+-----------------+----------------------------+
| ``GetPlayerInformation``| Address         | Player-         | query player's information |
|                         |                 |                 |                            |
|                         | player's address| Information     |                            |
+-------------------------+-----------------+-----------------+----------------------------+

Write Contract
--------------

Use the code generator to generate contracts and test projects
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Open the AElf.Boilerplate.CodeGenerator project in the AElf.Boilerplate
solution, and modify the Contents node in appsetting.json under this
project:

.. code:: json

   {
     "Contents": [
     {
       "Origin": "AElf.Contracts.HelloWorldContract",
       "New": "AElf.Contracts.BingoContract"
     },
     {
       "Origin": "HelloWorld",
       "New": "Bingo"
     },
     {
       "Origin": "hello_world",
       "New": "bingo"
     }
     ],
   }

Then run the AElf.Boilerplate.CodeGenerator project. After running
successfully, you will see a AElf.Contracts.BingoContract.sln in the
same directory as the AElf.Boilerplate.sln is in. After opening the sln,
you will see that the contract project and test case project of the
Bingo contract have been generated and are included in the new solution.

Define Proto
~~~~~~~~~~~~

Based on the API list in the requirements analysis, the
bingo_contract.proto file is as follows:

.. code:: proto

   syntax = "proto3";
   import "aelf/core.proto";
   import "aelf/options.proto";
   import "google/protobuf/empty.proto";
   import "google/protobuf/wrappers.proto";
   import "google/protobuf/timestamp.proto";
   option csharp_namespace = "AElf.Contracts.BingoContract";
   service BingoContract {
       option (aelf.csharp_state) = "AElf.Contracts.BingoContract.BingoContractState";

       // Actions
       rpc Register (google.protobuf.Empty) returns (google.protobuf.Empty) {
       }
       rpc Play (google.protobuf.Int64Value) returns (google.protobuf.Int64Value) {
       }
       rpc Bingo (aelf.Hash) returns (google.protobuf.BoolValue) {
       }
       rpc Quit (google.protobuf.Empty) returns (google.protobuf.Empty) {
       }

       // Views
       rpc GetAward (aelf.Hash) returns (google.protobuf.Int64Value) {
           option (aelf.is_view) = true;
       }
       rpc GetPlayerInformation (aelf.Address) returns (PlayerInformation) {
           option (aelf.is_view) = true;
       }
   }
   message PlayerInformation {
       aelf.Hash seed = 1;
       repeated BoutInformation bouts = 2;
       google.protobuf.Timestamp register_time = 3;
   }
   message BoutInformation {
       int64 play_block_height = 1;
       int64 amount = 2;
       int64 award = 3;
       bool is_complete = 4;
       aelf.Hash play_id = 5;
       int64 bingo_block_height = 6;
   }

Contract Implementation
~~~~~~~~~~~~~~~~~~~~~~~

Here only talk about the general idea of the Action method, specifically
need to turn the code:

https://github.com/AElfProject/aelf-boilerplate/blob/dev/chain/contract/AElf.Contracts.BingoGameContract/BingoGameContract.cs

Register & Quit
^^^^^^^^^^^^^^^

Register：

1. Determine the Seed of the user, Seed is a hash value, participating
   in the calculation of the random number, each user is different, so
   as to ensure that different users get different results on the same
   height;
2. Record the user’s registration time.

Quit：Just delete the user’s information.

Play & Bingo
^^^^^^^^^^^^

Play：

1. Use TransferFrom to deduct the user’s bet amount;
2. At the same time add a round (Bount) for the user, when the Bount is
   initialized, record three messages： 1.PlayId, the transaction Id of
   this transaction, is used to uniquely identify the Bout (see
   BoutInformation for its data structure in the Proto definition);
   2.Amount，Record the amount of the bet； 3.Record the height of the
   block in which the Play transaction is packaged.

Bingo：

1. Find the corresponding Bout according to PlayId, if the current block
   height is greater than PlayBlockHeight + number of nodes \* 8, you
   can get the result that you win or lose;
2. Use the current height and the user’s Seed to calculate a random
   number, and then treat the hash value as a bit Array, each of which
   is added to get a number ranging from 0 to 256.
3. Whether the number is divisible by 2 determines the user wins or
   loses;
4. The range of this number determines the amount of win/loss for the
   user, see the note of GetKind method for details.

Write Test
----------

Because the token transfer is involved in this test, in addition to
constructing the stub of the bingo contract, the stub of the token
contract is also required, so the code referenced in csproj for the
proto file is:

.. code:: text

   <ItemGroup>
     <ContractStub Include="..\..\protobuf\bingo_contract.proto">
       <Link>Protobuf\Proto\bingo_contract.proto</Link>
     </ContractStub>
     <ContractStub Include="..\..\protobuf\token_contract.proto">
       <Link>Protobuf\Proto\token_contract.proto</Link>
     </ContractStub>
   </ItemGroup>

Then you can write test code directly in the Test method of
BingoContractTest. Prepare the two stubs mentioned above:

.. code:: c#

   // Get a stub for testing.
   var keyPair = SampleECKeyPairs.KeyPairs[0];
   var stub = GetBingoContractStub(keyPair);
   var tokenStub =
       GetTester<TokenContractContainer.TokenContractStub>(
           GetAddress(TokenSmartContractAddressNameProvider.StringName), keyPair);

The stub is the stub of the bingo contract, and the tokenStub is the
stub of the token contract.

In the unit test, the keyPair account is given a large amount of ELF by
default, and the bingo contract needs a certain bonus pool to run, so
first let the account transfer ELF to the bingo contract:

.. code:: c#

   // Prepare awards.
   await tokenStub.Transfer.SendAsync(new TransferInput
   {
       To = DAppContractAddress,
       Symbol = "ELF",
       Amount = 100_00000000
   });

Then you can start using the Bingo contract. Register：

.. code:: c#

   await stub.Register.SendAsync(new Empty());

After registration, take a look at PlayInformation:

.. code:: c#

   // Now I have player information.
   var address = Address.FromPublicKey(keyPair.PublicKey);
   {
       var playerInformation = await stub.GetPlayerInformation.CallAsync(address);
       playerInformation.Seed.Value.ShouldNotBeEmpty();
       playerInformation.RegisterTime.ShouldNotBeNull();
   }

Bet, but before you can bet, you need to Approve the bingo contract:

.. code:: c#

   // Play.
   await tokenStub.Approve.SendAsync(new ApproveInput
   {
       Spender = DAppContractAddress,
       Symbol = "ELF",
       Amount = 10000
   });
   await stub.Play.SendAsync(new Int64Value {Value = 10000});

See if Bout is generated after betting.

.. code:: c#

   Hash playId;
   {
       var playerInformation = await stub.GetPlayerInformation.CallAsync(address);
       playerInformation.Bouts.ShouldNotBeEmpty();
       playId = playerInformation.Bouts.First().PlayId;
   }

Since the outcome requires eight blocks, you need send seven invalid
transactions (these transactions will fail, but the block height will
increase) :

.. code:: c#

   // Mine 7 more blocks.
   for (var i = 0; i < 7; i++)
   {
       await stub.Bingo.SendWithExceptionAsync(playId);
   }

Last check the award, and that the award amount is greater than 0 indicates you win.

.. code:: c#

   await stub.Bingo.SendAsync(playId);
   var award = await stub.GetAward.CallAsync(playId);
   award.Value.ShouldNotBe(0);

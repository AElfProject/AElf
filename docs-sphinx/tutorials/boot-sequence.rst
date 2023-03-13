AElf Blockchain Boot Sequence
=============================

This section mainly explains how the AElf Blockchain starts from the
initial nodes, and gradually replaces the initial nodes with true
production nodes through elections, thus completing the complete process
of AElf Blockchain startup.

Start initial nodes
----------------------------------------------------------------------------

We need to start at least one or more initial nodes to start the AElf
Blockchain, and 1-5 initial nodes are recommended.

In the Getting Started section, we described the steps to start multiple
nodes, you can follow the :doc:`Running multi-nodes with
Docker <../getting-started/development-environment/node>` 
to complete the initial nodes startup (this section also takes the
example of starting three initial nodes).

Since the default period of election time is 604800 seconds(7 days), if
you want to see the result of the election more quickly, modify the
configuration file appsettings.json before starting the boot nodes to
set the PeriodSeconds to smaller:

.. code:: json

   {
     "Consensus": {
       "PeriodSeconds": 604800
     },
   }

Run full node
--------------------------------------------------------------------------------------------

Create an account for the full node:
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code:: bash

   aelf-command create

   AElf [Info]: Your wallet info is : 
   AElf [Info]: Mnemonic            : major clap hurdle hammer push slogan ranch quantum reunion hope enroll repeat 
   AElf [Info]: Private Key         : 2229945cf294431183fd1d8101e27b17a1a590d3a1f7f2b9299850b24262ed8a 
   AElf [Info]: Public Key          : 04eed00eb009ccd283798e3862781cebd25ed6a4641e0e1b7d0e3b6b59025040679fc4dc0edc9de166bd630c7255188a9aeadfc832fdae0828270f77c6ef267905 
   AElf [Info]: Address             : Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC

Start full node:
~~~~~~~~~~~~~~~~

The startup steps for the full node are similar to the initial node
startup, but the configuration file section notes that the
InitialMinerList needs to be consistent with the initial node:

.. code:: json

   {
     "InitialMinerList" : [
         "0499d3bb14337961c4d338b9729f46b20de8a49ed38e260a5c19a18da569462b44b820e206df8e848185dac6c139f05392c268effe915c147cde422e69514cc927",
         "048397dfd9e1035fdd7260329d9492d88824f42917c156aef93fd7c2e3ab73b636f482b8ceb5cb435c556bfa067445a86e6f5c3b44ae6853c7f3dd7052609ed40b",
         "041cc962a51e7bbdd829a8855eca8a03fda708fdf31969251321cb31edadd564bf3c6e7ab31b4c1f49f0f206be81dbe68a75c70b293bf9d04d867ee5e415d3bf8a"
     ],
   }

Full node started successfully:
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

By checking the current node state, it can be seen that the full node is
synchronizing, and the BestChainHeight and the
LastIrreversibleBlockHeight are growing up. After catching up with the
height of the initial node, the subsequent steps can be carried out.

.. code:: bash

   aelf-command get-chain-status
    
   {
     "ChainId": "AELF",
     "Branches": {
       "fb749177c2f43db8c7d73ea050240b9f870c40584f044b13e7ec146c460b0eff": 2449
     },
     "NotLinkedBlocks": {},
     "LongestChainHeight": 2449,
     "LongestChainHash": "fb749177c2f43db8c7d73ea050240b9f870c40584f044b13e7ec146c460b0eff",
     "GenesisBlockHash": "ea9c0b026bd638ceb38323eb71174814c95333e39c62936a38c4e01a8f18062e",
     "GenesisContractAddress": "pykr77ft9UUKJZLVq15wCH8PinBSjVRQ12sD1Ayq92mKFsJ1i",
     "LastIrreversibleBlockHash": "66638f538038bd56357f3cf205424e7393c5966830ef0d16a75d4a117847e0bc",
     "LastIrreversibleBlockHeight": 2446,
     "BestChainHash": "fb749177c2f43db8c7d73ea050240b9f870c40584f044b13e7ec146c460b0eff",
     "BestChainHeight": 2449
   }

Be a candidate node
-------------------------------------------------------------------------------

Full nodes need to call Election contract to become candidate nodes. The
nodes need to mortgage 10W ELF to participate in the election, please
make sure that the account of the nodes has enough tokens.

To facilitate the quick demonstration, we directly transfer the token
from the first initial node account to the full node account:

.. code:: bash

   aelf-command send AElf.ContractNames.Token Transfer '{"symbol": "ELF", "to": "Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC", "amount": "20000000000000"}'

By checking the balance of the full node account, we can see that the
full node account has enough tokens, 20W ELF:

.. code:: bash

   aelf-command call AElf.ContractNames.Token GetBalance '{"symbol": "ELF", "owner": "Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC"}'
    
   Result:
   {
     "symbol": "ELF",
     "owner": "Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC",
     "balance": "20000000000000"
   } 

Full node announces election with admin specified in params:

.. code:: bash

   aelf-command send AElf.ContractNames.Election AnnounceElection '{"value": "Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC"}' -a Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC

By inquiring candidate information, we can see the full node is already
candidates:

.. code:: bash

   aelf-command call AElf.ContractNames.Election GetCandidateInformation '{"value":"04eed00eb009ccd283798e3862781cebd25ed6a4641e0e1b7d0e3b6b59025040679fc4dc0edc9de166bd630c7255188a9aeadfc832fdae0828270f77c6ef267905"}'
    
   Result:
   {
     "terms": [],
     "pubkey": "04eed00eb009ccd283798e3862781cebd25ed6a4641e0e1b7d0e3b6b59025040679fc4dc0edc9de166bd630c7255188a9aeadfc832fdae0828270f77c6ef267905",
     "producedBlocks": "0",
     "missedTimeSlots": "0",
     "continualAppointmentCount": "0",
     "announcementTransactionId": "8cc8eb5de35e390e4f7964bbdc7edc433498b041647761361903c6165b9f8659",
     "isCurrentCandidate": true
   } 

User vote election
------------------------------------------------------------------------------

For the simulated user voting scenario, we create a user account:

.. code:: bash

   aelf-command create
    
   AElf [Info]: Your wallet info is : 
   AElf [Info]: Mnemonic            : walnut market museum play grunt chuckle hybrid accuse relief misery share meadow 
   AElf [Info]: Private Key         : 919a220fac2d80e674a256f2367ac840845f344269f4dcdd56d37460de17f947 
   AElf [Info]: Public Key          : 04794948de40ffda2a6c884d7e6a99bb8e42b8b96b9ee5cc4545da3a1d5f7725eec93de62ddbfb598ef6f04fe52aa310acc7d17abeeea3946622573c4b0b2433ac 
   AElf [Info]: Address             : ZBBPU7DMVQ72YBQNmaKTDPKaAkHNzzA3naH5B6kE7cBm8g1ei

After the user account is created successfully, we will first trsnfer
some tokens to the account for voting.

.. code:: bash

   aelf-command send AElf.ContractNames.Token Transfer '{"symbol": "ELF", "to": "ZBBPU7DMVQ72YBQNmaKTDPKaAkHNzzA3naH5B6kE7cBm8g1ei", "amount": "200000000000"}'

Confirm the tokens has been received:

.. code:: bash

   aelf-command call AElf.ContractNames.Token GetBalance '{"symbol": "ELF", "owner": "ZBBPU7DMVQ72YBQNmaKTDPKaAkHNzzA3naH5B6kE7cBm8g1ei"}'
    
   Result:
   {
     "symbol": "ELF",
     "owner": "ZBBPU7DMVQ72YBQNmaKTDPKaAkHNzzA3naH5B6kE7cBm8g1ei",
     "balance": "200000000000"
   } 

Users vote on candidate nodes through the election contract.

.. code:: bash

   aelf-command send AElf.ContractNames.Election Vote '{"candidatePubkey":"04eed00eb009ccd283798e3862781cebd25ed6a4641e0e1b7d0e3b6b59025040679fc4dc0edc9de166bd630c7255188a9aeadfc832fdae0828270f77c6ef267905","amount":2000000000,"endTimestamp":{"seconds":1600271999,"nanos":999000}}' -a ZBBPU7DMVQ72YBQNmaKTDPKaAkHNzzA3naH5B6kE7cBm8g1ei

By inquiring the votes of candidates, we can see that the full node has
successfully obtained 20 votes.

.. code:: bash


   aelf-command call AElf.ContractNames.Election GetCandidateVote '{"value":"04eed00eb009ccd283798e3862781cebd25ed6a4641e0e1b7d0e3b6b59025040679fc4dc0edc9de166bd630c7255188a9aeadfc832fdae0828270f77c6ef267905"}'
    
   Result:
   {
     "obtainedActiveVotingRecordIds": [
       "172375e9cee303ce60361aa73d7326920706553e80f4485f97ffefdb904486f1"
     ],
     "obtainedWithdrawnVotingRecordIds": [],
     "obtainedActiveVotingRecords": [],
     "obtainedWithdrawnVotesRecords": [],
     "obtainedActiveVotedVotesAmount": "2000000000",
     "allObtainedVotedVotesAmount": "2000000000",
     "pubkey": "BO7QDrAJzNKDeY44Yngc69Je1qRkHg4bfQ47a1kCUEBnn8TcDtyd4Wa9YwxyVRiKmurfyDL9rggoJw93xu8meQU="
   } 

Become production node
----------------------------------------------------------------------------------

At the next election, the candidate nodes with votes in the first 17 are
automatically elected as production nodes, and the current production
node list can be viewed through consensus contracts.

Quantity 17 is the default maximum production node quantity, which can
be modified by proposal. Please refer to the Consensus and Proposal
Contract API for details.

.. code:: bash

   aelf-command call AElf.ContractNames.Consensus GetCurrentMinerPubkeyList '{}'

   Result:
   {
     "pubkeys": [
        "0499d3bb14337961c4d338b9729f46b20de8a49ed38e260a5c19a18da569462b44b820e206df8e848185dac6c139f05392c268effe915c147cde422e69514cc927",
        "048397dfd9e1035fdd7260329d9492d88824f42917c156aef93fd7c2e3ab73b636f482b8ceb5cb435c556bfa067445a86e6f5c3b44ae6853c7f3dd7052609ed40b",
        "041cc962a51e7bbdd829a8855eca8a03fda708fdf31969251321cb31edadd564bf3c6e7ab31b4c1f49f0f206be81dbe68a75c70b293bf9d04d867ee5e415d3bf8a",
        "04eed00eb009ccd283798e3862781cebd25ed6a4641e0e1b7d0e3b6b59025040679fc4dc0edc9de166bd630c7255188a9aeadfc832fdae0828270f77c6ef267905"
     ]
   } 

Add more production nodes
-------------------------------------------------------------------------------------

Repeat steps 2-4 to add more production nodes. When the number of
initial nodes plus the number of candidate nodes exceeds the maximum
number of production node, the replacement will replace the initial
nodes step by step, and the replaced initial nodes are not allowed to
run for election again. At this time, the initial node has completed its
responsibility of starting AElf Blockchain.

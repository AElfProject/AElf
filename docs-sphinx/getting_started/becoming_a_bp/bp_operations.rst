Operations
==========

Set up a full node
------------------

aelf doesn't have light nodes, it means that all the nodes users set up are full nodes. You can click `here <https://docs.aelf.io/en/latest/getting_started/development-environment/node.html#multi-nodes>`_ to learn how to set up a full node. Please note: When you simulate it in the local environment, the BP election mechanism won't be triggered if you only set up a single node, which means the node can't become a BP. Hence, you need to set up **at least three nodes** in the local environment.

Please note: The term that aelf's BPs are elected is every ``7`` days. To make this tutorial concise and easy to follow, it will change the configuration of the term to 120s. You will find and follow the instructions in the following guide.
 
1. Find the ``appsettings.json`` file in ``/.../ src / AElf.Launcher`` and you will see the code below:

   .. code:: bash

      "Consensus": {
         "InitialMinerList": ["04884d9563b3b67a5*****0cba710d956718*****"],
         "MiningInterval": 4000,
         "StartTimestamp": 0,
         "PeriodSeconds": 604800,
         "MinerIncreaseInterval": 31536000
      }

2. Configure the ``Public Key`` of the three nodes and separate them with ``,`` like this:

   .. code:: bash

      "InitialMinerList" : [
         "04884d9563b3b67a5*****526dd489e3805211cba710d956718*****",
         "045670526219d7315*****8629891b0617ab605e646ae78961c*****",
         "046a5913eae5fee3d*****3826beb2b7109b5141679a1927338*****"
      ],

3. Change ``"PeriodSeconds": 604800`` to ``"PeriodSeconds": 120`` and the election term will be changed to 2 minutes.

4. If you have set up nodes and produced blocks before, please shut down your nodes and delete all Redis data via command, the instructions of which can be found in `Multi-Nodes <https://docs.aelf.io/en/latest/getting_started/development-environment/node.html#multi-nodes>`_ . After that, you can restart your multi-nodes again.


Become a candidate node
-----------------------

You need to stake 100,000 ELF in order to participate in the node election. Please make sure that you are not low on balance.

- **Mainnet**

You can enter the Governance page `here <https://explorer.aelf.io/vote/election>`__, click the "Become a candidate node" button, and stake 100,000 ELF to join the node election.

- **Simulation in the local environment**

1. Execute this command to check if you have enough balance in the node. All the ELF tokens generated in the local environment are stored in the first node address that you configured in ``InitialMinerList`` in step 1, i.e. "**Set up a full node**". 
   
   Please note: In the following demonstration, we will be using the address ``Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC`` and its public key is ``04eed00eb009ccd283798e3...828270f77c6ef267905``. You need to replace it with your own address when you operate it.

   .. code:: bash

      aelf-command call AElf.ContractNames.Token GetBalance '{"symbol": "ELF", "owner": "Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC"}'

   When the command is executed, you will see info like this and ``balance`` here refers to the balance in this address.

   .. code:: bash

      Result:
      {
         "symbol": "ELF",
         "owner": "Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC",
         "balance": "20000000000000"
      }

2. Skip this step if the balance is greater than 200,000 ELF. While if it's not, you can transfer 200,000 ELF to it using the following command:

   .. code:: bash

      aelf-command send AElf.ContractNames.Token Transfer '{"symbol": "ELF", "to": "Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC", "amount": "20000000000000"}'

   After that, you can run the command in step 1 to check the balance.

3. Execute this command so that the full node announces that it will join the node election and appoints an admin:

   .. code:: bash

      aelf-command send AElf.ContractNames.Election AnnounceElection '{"value": "Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC"}' -a Q3t34SAEsxAQrSQidTRzDonWNTPpSTgH8bqu8pQUGCSWRPdRC

4. Execute this command to check the candidate node's info:

   .. code:: bash

      aelf-command call AElf.ContractNames.Election GetCandidateInformation '{"value":"04eed00eb009ccd283798e3862781cebd25ed6a4641e0e1b7d0e3b6b59025040679fc4dc0edc9de166bd630c7255188a9aeadfc832fdae0828270f77c6ef267905"}'

   When the command is executed, you will see that the public key of the full node is on the candidate list, meaning it's a candidate node.

   .. code:: bash

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

Users vote for nodes
--------------------

- **Mainnet**

Users can visit `this site <https://explorer.aelf.io/vote/election>`__ and vote for candidate nodes at Governance - Vote - Node Table. The top 2N+1 nodes will be elected as the BPs, where N starts from 8 in 2022 and increases by 1 each year.

- **Simulation in the local environment**

1. Execute this command to create a user account to simulate voting:

   .. code:: bash

      aelf-command create

   The account info is as follows:

   .. code:: bash

      AElf [Info]: Your wallet info is :
      AElf [Info]: Mnemonic            : walnut market museum play grunt chuckle hybrid accuse relief misery share meadow
      AElf [Info]: Private Key         : 919a220fac2d80e674a256f2367ac840845f344269f4dcdd56d37460de17f947
      AElf [Info]: Public Key          : 04794948de40ffda2a6c884d7e6a99bb8e42b8b96b9ee5cc4545da3a1d5f7725eec93de62ddbfb598ef6f04fe52aa310acc7d17abeeea3946622573c4b0b2433ac
      AElf [Info]: Address             : ZBBPU7DMVQ72YBQNmaKTDPKaAkHNzzA3naH5B6kE7cBm8g1ei

2. Execute this command to transfer some tokens to it for voting purposes (2000 ELF is used here for demonstration).

   .. code:: bash

      aelf-command send AElf.ContractNames.Token Transfer '{"symbol": "ELF", "to": "ZBBPU7DMVQ72YBQNmaKTDPKaAkHNzzA3naH5B6kE7cBm8g1ei", "amount": "200000000000"}'

3. Execute this command to check the balance of this newly-created account:

   .. code:: bash

      aelf-command call AElf.ContractNames.Token GetBalance '{"symbol": "ELF", "owner": "ZBBPU7DMVQ72YBQNmaKTDPKaAkHNzzA3naH5B6kE7cBm8g1ei"}'

   The result shows that it has a balance of 2000 ELF, meaning the tokens have been received.

   .. code:: bash

      Result:
      {
         "symbol": "ELF",
         "owner": "ZBBPU7DMVQ72YBQNmaKTDPKaAkHNzzA3naH5B6kE7cBm8g1ei",
         "balance": "200000000000"
      }

4. Execute this command to vote for the candidate node via the election contract (20 ELF is used here for demonstration). ``candidatePubkey`` is the public key of the candidate node:

   .. code:: bash

      aelf-command send AElf.ContractNames.Election Vote '{"candidatePubkey":"04eed00eb009ccd283798e3862781cebd25ed6a4641e0e1b7d0e3b6b59025040679fc4dc0edc9de166bd630c7255188a9aeadfc832fdae0828270f77c6ef267905","amount":2000000000,"endTimestamp":{"seconds":1600271999,"nanos":999000}}' -a ZBBPU7DMVQ72YBQNmaKTDPKaAkHNzzA3naH5B6kE7cBm8g1ei

5. Execute this command to check the number of votes the candidate received:

   .. code:: bash

      aelf-command call AElf.ContractNames.Election GetCandidateVote '{"value":"04eed00eb009ccd283798e3862781cebd25ed6a4641e0e1b7d0e3b6b59025040679fc4dc0edc9de166bd630c7255188a9aeadfc832fdae0828270f77c6ef267905"}'

   After it's executed, the result will be as follows. Here, the full node has received 20 ELF as votes.

   .. code:: bash

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

Become a BP
-----------

The top 2N+1 candidate nodes will automatically be elected as BPs in the next term. A list of the public keys of the current BPs' can be obtained via the consensus contract.

Execute this command:

.. code:: bash

   aelf-command call AElf.ContractNames.Consensus GetCurrentMinerPubkeyList '{}'

Info of the current BPs will be returned:

.. code:: bash

      Result:
      {
         "pubkeys": [
            "0499d3bb14337961c4d338b9729f46b20de8a49ed38e260a5c19a18da569462b44b820e206df8e848185dac6c139f05392c268effe915c147cde422e69514cc927",
            "048397dfd9e1035fdd7260329d9492d88824f42917c156aef93fd7c2e3ab73b636f482b8ceb5cb435c556bfa067445a86e6f5c3b44ae6853c7f3dd7052609ed40b",
            "041cc962a51e7bbdd829a8855eca8a03fda708fdf31969251321cb31edadd564bf3c6e7ab31b4c1f49f0f206be81dbe68a75c70b293bf9d04d867ee5e415d3bf8a",
            "04eed00eb009ccd283798e3862781cebd25ed6a4641e0e1b7d0e3b6b59025040679fc4dc0edc9de166bd630c7255188a9aeadfc832fdae0828270f77c6ef267905"
         ]
      }

Add more BPs
------------

You can repeat steps 1-4 to add more BPs, but you don't need to edit the configuration file ``appsettings.json`` in step 1 again. When the number of genesis nodes and candidate nodes exceeds the maximum number of BPs, the candidate nodes will gradually replace the genesis nodes and the replaced genesis nodes can't participate in node election again. After all the genesis nodes are replaced, they will have fulfilled their duty of starting aelf Mainnet.

If you have learned about how to become a BP, you can proceed with the following docs for contract deployment and DApp development guide.

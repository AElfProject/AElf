Operations
==========

The process of becoming a BP is as follows:

1. Interested parties set up nodes.

2. Nodes participate in the BP election.

3. Voters stake ELF tokens to vote for their preferred nodes.

4. BPs are elected based on the number of votes they accumulate and serve a seven-day term until the next election cycle begins. (The top 2N+1 nodes become BPs, and the top 5*(2N+1) nodes become candidate nodes).

Set up nodes
------------

aelf currently doesn't have light nodes, it means that all the nodes users set up are full nodes. You can click `here <https://docs.aelf.io/en/latest/tutorials/mainnet.html>`__ to learn how to set up a full node. 

Note: Since you want to become a BP, you need to run individual nodes for both MainChain AELF and all the SideChains. 

Participate in BP election
--------------------------

You need to stake 100,000 ELF in order to participate in the node election. Please make sure that you are not low on balance.

You can enter the `Governance page <https://explorer.aelf.io/vote/election>`__, click the "Become a candidate node" button, and stake 100,000 ELF to join the node election.

Users vote for nodes
--------------------

Users can visit `this site <https://explorer.aelf.io/vote/election>`__ and vote for candidate nodes at Governance - Vote - Node Table.

The top 2N+1 nodes become BPs, and the top 5*(2N+1) nodes become candidate nodes, where N starts from 8 in 2022 and increases by 1 each year.

BPs are elected
---------------

BPs are elected every seven days and the change of terms starts at 7:23 (UTC) every Thursday. If your node receives enough votes to rank top 2N+1 of all candidate nodes in the election, it will automatically become a BP in the next term. If ranking between top 2N+1 and top 5*(2N+1), it will become a candidate node. You can check the current elected BPs in real-time on the `election page <https://explorer.aelf.io/vote/election>`__.

Simulate in the local environment
---------------------------------

If you want to try setting up a node and running as a BP in your local environment, please follow the `instructions <../../getting_started/becoming_a_bp/simulation_in_the_local_environment.html>`__ to simulate it in the local.
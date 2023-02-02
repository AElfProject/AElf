Deploying Smart Contracts
=========================

Contracts deployment can be done by 2 means: one is via aelf explorer
while the other is via aelf-command. Before you start deploying, please
make sure that you have installed npm and aelf-command. If you haven’t
completed it, please follow the `Deployment
Environment <这里放环境准备的线上链接>`__ here. ## Overview

Contract deployment can be broken down into 5 steps:

1. For developers, they will initiate 3 transactions in the following
   order.

   1. ``ProposeNewContract``\ ：Apply to deploy the contract

      After the application is submitted, the proposal needs to be
      approved by the BPs before the next step can begin.

   2. ``ReleaseApprovedContract``\ ：Apply for code check

      After the application is submitted, BPs will need to run their
      nodes and automatically check the code. Then the
      ``ApproveMultiProposals`` transaction will be initiated before the
      next step can begin.

   3. ``ReleaseCodeCheckedContract``\ ：Execute the code of contract
      deployment

2. For BPs, they will have 2 tasks:

   1. Approve the application of contract deployment.
   2. Run aelf’s nodes and automatically check the code. The
      ``ApproveMultiProposals`` transaction will be automatically
      initiated.

Deploy through aelf Explorer
----------------------------

Notes: This doc only illustrates the procedure of contract deployment on
AElf Mainnet, that is, when ``ContractDeploymentAuthorityRequired`` is
true. Please make sure that you have created an AElf wallet and
possessed around 100 ELF before you start deploying. When
``ContractDeploymentAuthorityRequired`` is false, you can directly
complete deployment and upgrade via ``DeploySmartContract`` and
``UpdateSmartContract`` in Contract Zero.

Click
`here <https://medium.com/aelfblockchain/tutorial-how-to-manage-contracts-with-aelf-explorer-v1-2-0-2dcc36b439d9>`__
to learn contract deployment through aelf Explorer.

Deploy through aelf-command
---------------------------

This section will walk you through the procedure of contract deployment
through aelf-command. As ``DeploySmartContract`` and
``UpdateSmartContract`` are similar in operation, the following
instructions will use the first one as an example.

.. figure:: img/philly-magic-garden.jpg
   :alt: 合约部署流程

   流程图

Developer: ProposeNewContract
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

-  Developers Initiate the ``ProposeNewContract`` transaction.
-  A ``ProposeContractCodeCheck`` proposal will be created via the
   ``ProposeNewContract`` transaction and the BPs will be asked to check
   the code.
-  Once ``ProposeContractCodeCheck`` is executed, another
   ``ProposeContractCodeCheck`` proposal will be created.

BP: Parliament.Approve
~~~~~~~~~~~~~~~~~~~~~~

-  BPs manually approve the ``ProposeContractCodeCheck`` proposal
   submitted by the developers, agreeing to check the contract code.

Developer: ReleaseApprovedContract
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

-  Once the ``ProposeContractCodeCheck`` proposal is approved by
   parliament, it will be released via the ``ReleaseApprovedContract``
   transaction.
-  A ``ProposeContractCodeCheck`` proposal will then be created, asking
   the BPs to automatically check the code (an event will be raised and
   handled on-chain, and code check will be executed). After the
   proposal is approved, the ``DeploySmartContract`` method will be
   executed.

BP: Parliament.ApproveMultiProposals (Auto)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

-  BPs will run aelf’s nodes and check the contract code. If the code
   passes the review, the ``ApproveMultiProposals`` transaction will be
   automatically initiated via the system. That is, the
   ``ProposeContractCodeCheck`` proposal is approved.

Developer: ReleaseCodeCheckedContract
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

-  The ``ProposeContractCodeCheck`` proposal will be released via the
   ``ReleaseCodeCheckedContract`` transaction. The
   ``DeploySmartContract`` method will be executed and then contract
   deployment is done.

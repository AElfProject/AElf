Deploying|Updating Contracts (No BP Approval Required)
========================================================================

Contracts deployment/update can be done by 2 means: one is via aelf
explorer while the other is via aelf-command. Before you start
deploying/updating, please make sure that you have installed npm and
aelf-command. If you haven’t completed it, please follow the
`Deployment Environment <../../getting_started/smart_contracts/development_environment.html>`__ here. 

Overview
--------

In the following 6 situations, you can choose to deploy/update
contracts without BPs' approval. Please note that for different
conditions, the requirement for initiators differs.

1. Deploying user contracts on shared SideChains, can be initiated
   by users or BPs.
2. Updating user contracts on shared SideChains, can only be
   initiated by contract creators.
3. Deploying user contracts on exclusive SideChains, can only be
   initiated by SideChain creators.
4. Updating user contracts on exclusive SideChains, can only be
   initiated by contract creators.
5. Deploying user contracts on MainChain, can only be initiated by
   BPs (The recommended contract deployment is on SideChains and we
   strongly encourage you to not deploy on MainChain). 
6. Updating user contracts on MainChain, can only be initiated by
   contract creators.

User contracts here refer to non-system contracts.
Please note that the prerequisite for successful deployments/updates
is that your contracts have implemented the ACS12 standards.


Compared with the procedure where BP approval is required, for
no-approval-needed contract deployment/update, developers only need
to initiate 1 transaction in the entire process.
.. figure:: No-BP-approval-required.png
:alt: 合约部署流程

Developer: DeployUserSmartContract / UpdateUserSmartContract
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Contract Deployment
"""""""""""""""""""

-  The developer initiates the ``DeployUserSmartContract``
   transaction.

-  A CodeCheck proposal will be created and the BPs will be asked
   to check the code.

-  The transaction returns ``CodeHash`` of the contract deployment.

Contract Update
"""""""""""""""

-  The developer initiates the ``UpdateUserSmartContract``
   transaction.

-  A CodeCheck proposal will be created and the BPs will be asked
   to check the code.


BP: Parliament.ApproveMultiProposals (automatic)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

-  BPs automatically complete the contract code check. If the code
   passes the check, an ApproveMultiProposals transaction will be
   initiated via the system, and that means the CodeCheck proposal
   is approved. 
   

BP: ReleaseApprovedUserSmartContract (automatic)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

-  Once the automatic code check passes by no less than 2/3 of BPs
   (rounding down) + 1, BPs will release the CodeCheck proposal by
   initiating the ``ReleaseApprovedUserSmartContract`` transaction.
   They will execute the ``PerformDeployUserSmartContract`` method
   and then contract deployment/update is done.

-  If the code check fails to pass, the deployment/update will be
   terminated.



Developer: GetSmartContractRegistrationByCodeHash
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

In the case of contract deployment/update, the developer can get the
deployed/updated contract address through this method:

-  Use the CodeHash returned in the
   ``DeployUserSmartContract/UpdateUserSmartContract`` transaction
   to check the address of the deployed contract through
   ``GetSmartContractRegistrationByCodeHash``.

-  Since contract deployment/update requires BPs to complete code
   checks, the result can only be obtained after at least one round
   of block production.


If errors exist in the contract deployment/update transaction, the
first transaction will fail and info about the error can be obtained
by checking the transaction results.

If the contract deployment/update transaction is executed yet the
deployed/updated contract address can not be checked through
``GetSmartContractRegistrationByCodeHash`` after 10 minutes, please
troubleshoot the problem from the following aspects:

-  Whether the contract has implemented the ACS12 standards;

-  Whether the contract development scaffold is the latest version.
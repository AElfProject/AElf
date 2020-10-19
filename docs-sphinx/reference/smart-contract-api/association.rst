AElf.Contracts.Association
--------------------------

Association contract.

Organizations established to achieve specific goals can use this
contract to cooperatively handle transactions within the organization

Implement AElf Standards ACS1 and ACS3.

+---------------+-----------------+------------------+-----------------+
| Method Name   | Request Type    | Response Type    | Description     |
+===============+=================+==================+=================+
| Creat         | `Creat          | `                | Create an       |
| eOrganization | eOrganizationIn | .aelf.Address <# | organization    |
|               | put <#Associati | aelf.Address>`__ | and return its  |
|               | on.CreateOrgani |                  | address.        |
|               | zationInput>`__ |                  |                 |
+---------------+-----------------+------------------+-----------------+
| CreateOr      | `CreateO        | `                | Creates an      |
| ganizationByS | rganizationBySy | .aelf.Address <# | organization by |
| ystemContract | stemContractInp | aelf.Address>`__ | system contract |
|               | ut <#Associatio |                  | and return its  |
|               | n.CreateOrganiz |                  | address.        |
|               | ationBySystemCo |                  |                 |
|               | ntractInput>`__ |                  |                 |
+---------------+-----------------+------------------+-----------------+
| AddMember     | `.a             | `.g              | Add             |
|               | elf.Address <#a | oogle.protobuf.E | organization    |
|               | elf.Address>`__ | mpty <#google.pr | members.        |
|               |                 | otobuf.Empty>`__ |                 |
+---------------+-----------------+------------------+-----------------+
| RemoveMember  | `.a             | `.g              | Remove          |
|               | elf.Address <#a | oogle.protobuf.E | organization    |
|               | elf.Address>`__ | mpty <#google.pr | members.        |
|               |                 | otobuf.Empty>`__ |                 |
+---------------+-----------------+------------------+-----------------+
| ChangeMember  | `ChangeMe       | `.g              | Replace         |
|               | mberInput <#Ass | oogle.protobuf.E | organization    |
|               | ociation.Change | mpty <#google.pr | member with a   |
|               | MemberInput>`__ | otobuf.Empty>`__ | new member.     |
+---------------+-----------------+------------------+-----------------+
| Ge            | `.a             | `Organizatio     | Get the         |
| tOrganization | elf.Address <#a | n <#Association. | organization    |
|               | elf.Address>`__ | Organization>`__ | according to    |
|               |                 |                  | the             |
|               |                 |                  | organization    |
|               |                 |                  | address.        |
+---------------+-----------------+------------------+-----------------+
| Ca            | `Creat          | `                | Calculate the   |
| lculateOrgani | eOrganizationIn | .aelf.Address <# | input and       |
| zationAddress | put <#Associati | aelf.Address>`__ | return the      |
|               | on.CreateOrgani |                  | organization    |
|               | zationInput>`__ |                  | address.        |
+---------------+-----------------+------------------+-----------------+
| SetMethodFee  | `Me             | `.g              | Set the method  |
|               | thodFees <#acs1 | oogle.protobuf.E | fees for the    |
|               | .MethodFees>`__ | mpty <#google.pr | specified       |
|               |                 | otobuf.Empty>`__ | method. Note    |
|               |                 |                  | that this will  |
|               |                 |                  | override all    |
|               |                 |                  | fees of the     |
|               |                 |                  | method.         |
+---------------+-----------------+------------------+-----------------+
| ChangeMethod  | `.Aut           | `.g              | Change the      |
| FeeController | horityInfo <#Au | oogle.protobuf.E | method fee      |
|               | thorityInfo>`__ | mpty <#google.pr | controller, the |
|               |                 | otobuf.Empty>`__ | default is      |
|               |                 |                  | parliament and  |
|               |                 |                  | default         |
|               |                 |                  | organization.   |
+---------------+-----------------+------------------+-----------------+
| GetMethodFee  | `.g             | `                | Query method    |
|               | oogle.protobuf. | MethodFees <#acs | fee information |
|               | StringValue <#g | 1.MethodFees>`__ | by method name. |
|               | oogle.protobuf. |                  |                 |
|               | StringValue>`__ |                  |                 |
+---------------+-----------------+------------------+-----------------+
| GetMethod     | `.goog          | `.A              | Query the       |
| FeeController | le.protobuf.Emp | uthorityInfo <#A | method fee      |
|               | ty <#google.pro | uthorityInfo>`__ | controller.     |
|               | tobuf.Empty>`__ |                  |                 |
+---------------+-----------------+------------------+-----------------+
| C             | `Creat          | `.aelf.Hash      | Create a        |
| reateProposal | eProposalInput  |  <#aelf.Hash>`__ | proposal for    |
|               | <#acs3.CreatePr |                  | which           |
|               | oposalInput>`__ |                  | organization    |
|               |                 |                  | members can     |
|               |                 |                  | vote. When the  |
|               |                 |                  | proposal is     |
|               |                 |                  | released, a     |
|               |                 |                  | transaction     |
|               |                 |                  | will be sent to |
|               |                 |                  | the specified   |
|               |                 |                  | contract.       |
|               |                 |                  | Return id of    |
|               |                 |                  | the newly       |
|               |                 |                  | created         |
|               |                 |                  | proposal.       |
+---------------+-----------------+------------------+-----------------+
| Approve       | `.aelf.Hash     | `.g              | Approve a       |
|               | <#aelf.Hash>`__ | oogle.protobuf.E | proposal        |
|               |                 | mpty <#google.pr | according to    |
|               |                 | otobuf.Empty>`__ | the proposal    |
|               |                 |                  | ID.             |
+---------------+-----------------+------------------+-----------------+
| Reject        | `.aelf.Hash     | `.g              | Reject a        |
|               | <#aelf.Hash>`__ | oogle.protobuf.E | proposal        |
|               |                 | mpty <#google.pr | according to    |
|               |                 | otobuf.Empty>`__ | the proposal    |
|               |                 |                  | ID.             |
+---------------+-----------------+------------------+-----------------+
| Abstain       | `.aelf.Hash     | `.g              | Abstain a       |
|               | <#aelf.Hash>`__ | oogle.protobuf.E | proposal        |
|               |                 | mpty <#google.pr | according to    |
|               |                 | otobuf.Empty>`__ | the proposal    |
|               |                 |                  | ID.             |
+---------------+-----------------+------------------+-----------------+
| Release       | `.aelf.Hash     | `.g              | Release a       |
|               | <#aelf.Hash>`__ | oogle.protobuf.E | proposal        |
|               |                 | mpty <#google.pr | according to    |
|               |                 | otobuf.Empty>`__ | the proposal ID |
|               |                 |                  | and send a      |
|               |                 |                  | transaction to  |
|               |                 |                  | the specified   |
|               |                 |                  | contract.       |
+---------------+-----------------+------------------+-----------------+
| C             | `               | `.g              | Change the      |
| hangeOrganiza | ProposalRelease | oogle.protobuf.E | thresholds      |
| tionThreshold | Threshold <#acs | mpty <#google.pr | associated with |
|               | 3.ProposalRelea | otobuf.Empty>`__ | proposals. All  |
|               | seThreshold>`__ |                  | fields will be  |
|               |                 |                  | overwritten by  |
|               |                 |                  | the input value |
|               |                 |                  | and this will   |
|               |                 |                  | affect all      |
|               |                 |                  | current         |
|               |                 |                  | proposals of    |
|               |                 |                  | the             |
|               |                 |                  | organization.   |
|               |                 |                  | Note: only the  |
|               |                 |                  | organization    |
|               |                 |                  | can execute     |
|               |                 |                  | this through a  |
|               |                 |                  | proposal.       |
+---------------+-----------------+------------------+-----------------+
| ChangeOrg     | `P              | `.g              | Change the      |
| anizationProp | roposerWhiteLis | oogle.protobuf.E | white list of   |
| oserWhiteList | t <#acs3.Propos | mpty <#google.pr | organization    |
|               | erWhiteList>`__ | otobuf.Empty>`__ | proposer. This  |
|               |                 |                  | method          |
|               |                 |                  | overrides the   |
|               |                 |                  | list of         |
|               |                 |                  | whitelisted     |
|               |                 |                  | proposers.      |
+---------------+-----------------+------------------+-----------------+
| Crea          | `CreateP        | `.aelf.Hash      | Create a        |
| teProposalByS | roposalBySystem |  <#aelf.Hash>`__ | proposal by     |
| ystemContract | ContractInput < |                  | system          |
|               | #acs3.CreatePro |                  | contracts, and  |
|               | posalBySystemCo |                  | return id of    |
|               | ntractInput>`__ |                  | the newly       |
|               |                 |                  | created         |
|               |                 |                  | proposal.       |
+---------------+-----------------+------------------+-----------------+
| ClearProposal | `.aelf.Hash     | `.g              | Remove the      |
|               | <#aelf.Hash>`__ | oogle.protobuf.E | specified       |
|               |                 | mpty <#google.pr | proposal. If    |
|               |                 | otobuf.Empty>`__ | the proposal is |
|               |                 |                  | in effect, the  |
|               |                 |                  | cleanup fails.  |
+---------------+-----------------+------------------+-----------------+
| GetProposal   | `.aelf.Hash     | `Proposal        | Get the         |
|               | <#aelf.Hash>`__ | Output <#acs3.Pr | proposal        |
|               |                 | oposalOutput>`__ | according to    |
|               |                 |                  | the proposal    |
|               |                 |                  | ID.             |
+---------------+-----------------+------------------+-----------------+
| ValidateOrga  | `.a             | `.google.pr      | Check the       |
| nizationExist | elf.Address <#a | otobuf.BoolValue | existence of an |
|               | elf.Address>`__ |  <#google.protob | organization.   |
|               |                 | uf.BoolValue>`__ |                 |
+---------------+-----------------+------------------+-----------------+
| V             | `V              | `.google.pr      | Check if the    |
| alidatePropos | alidateProposer | otobuf.BoolValue | proposer is     |
| erInWhiteList | InWhiteListInpu |  <#google.protob | whitelisted.    |
|               | t <#acs3.Valida | uf.BoolValue>`__ |                 |
|               | teProposerInWhi |                  |                 |
|               | teListInput>`__ |                  |                 |
+---------------+-----------------+------------------+-----------------+

Association.ChangeMemberInput
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+------------+----------------------------------+-------+-------------------------+
| Field      | Type                             | Label | Description             |
+============+==================================+=======+=========================+
| old_member | `aelf.Address <#aelf.Address>`__ |       | The old member address. |
+------------+----------------------------------+-------+-------------------------+
| new_member | `aelf.Address <#aelf.Address>`__ |       | The new member address. |
+------------+----------------------------------+-------+-------------------------+

Association.CreateOrganizationBySystemContractInput
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| organ       | `C       |             | The parameters of creating   |
| ization_cre | reateOrg |             | organization.                |
| ation_input | anizatio |             |                              |
|             | nInput < |             |                              |
|             | #Associa |             |                              |
|             | tion.Cre |             |                              |
|             | ateOrgan |             |                              |
|             | izationI |             |                              |
|             | nput>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| org         | `str     |             | The organization address     |
| anization_a | ing <#st |             | callback method which        |
| ddress_feed | ring>`__ |             | replies the organization     |
| back_method |          |             | address to caller contract.  |
+-------------+----------+-------------+------------------------------+

Association.CreateOrganizationInput
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| or          | `Organiz |             | Initial organization         |
| ganization_ | ationMem |             | members.                     |
| member_list | berList  |             |                              |
|             | <#Associ |             |                              |
|             | ation.Or |             |                              |
|             | ganizati |             |                              |
|             | onMember |             |                              |
|             | List>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| prop        | `a       |             | The threshold for releasing  |
| osal_releas | cs3.Prop |             | the proposal.                |
| e_threshold | osalRele |             |                              |
|             | aseThres |             |                              |
|             | hold <#a |             |                              |
|             | cs3.Prop |             |                              |
|             | osalRele |             |                              |
|             | aseThres |             |                              |
|             | hold>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| proposer    | `acs     |             | The proposer whitelist.      |
| _white_list | 3.Propos |             |                              |
|             | erWhiteL |             |                              |
|             | ist <#ac |             |                              |
|             | s3.Propo |             |                              |
|             | serWhite |             |                              |
|             | List>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| cre         | `a       |             | The creation token is for    |
| ation_token | elf.Hash |             | organization address         |
|             |  <#aelf. |             | generation.                  |
|             | Hash>`__ |             |                              |
+-------------+----------+-------------+------------------------------+

Association.MemberAdded
~~~~~~~~~~~~~~~~~~~~~~~

+-------------------+-------------------+-------+-------------------+
| Field             | Type              | Label | Description       |
+===================+===================+=======+===================+
| member            | `aelf.Address <   |       | The added member  |
|                   | #aelf.Address>`__ |       | address.          |
+-------------------+-------------------+-------+-------------------+
| org               | `aelf.Address <   |       | The organization  |
| anization_address | #aelf.Address>`__ |       | address.          |
+-------------------+-------------------+-------+-------------------+

Association.MemberChanged
~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------------+-------------------+-------+-------------------+
| Field             | Type              | Label | Description       |
+===================+===================+=======+===================+
| old_member        | `aelf.Address <   |       | The old member    |
|                   | #aelf.Address>`__ |       | address.          |
+-------------------+-------------------+-------+-------------------+
| new_member        | `aelf.Address <   |       | The new member    |
|                   | #aelf.Address>`__ |       | address.          |
+-------------------+-------------------+-------+-------------------+
| org               | `aelf.Address <   |       | The organization  |
| anization_address | #aelf.Address>`__ |       | address.          |
+-------------------+-------------------+-------+-------------------+

Association.MemberRemoved
~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------------+-------------------+-------+-------------------+
| Field             | Type              | Label | Description       |
+===================+===================+=======+===================+
| member            | `aelf.Address <   |       | The removed       |
|                   | #aelf.Address>`__ |       | member address.   |
+-------------------+-------------------+-------+-------------------+
| org               | `aelf.Address <   |       | The organization  |
| anization_address | #aelf.Address>`__ |       | address.          |
+-------------------+-------------------+-------+-------------------+

Association.Organization
~~~~~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| or          | `Organiz |             | The organization members.    |
| ganization_ | ationMem |             |                              |
| member_list | berList  |             |                              |
|             | <#Associ |             |                              |
|             | ation.Or |             |                              |
|             | ganizati |             |                              |
|             | onMember |             |                              |
|             | List>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| prop        | `a       |             | The threshold for releasing  |
| osal_releas | cs3.Prop |             | the proposal.                |
| e_threshold | osalRele |             |                              |
|             | aseThres |             |                              |
|             | hold <#a |             |                              |
|             | cs3.Prop |             |                              |
|             | osalRele |             |                              |
|             | aseThres |             |                              |
|             | hold>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| proposer    | `acs     |             | The proposer whitelist.      |
| _white_list | 3.Propos |             |                              |
|             | erWhiteL |             |                              |
|             | ist <#ac |             |                              |
|             | s3.Propo |             |                              |
|             | serWhite |             |                              |
|             | List>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| organizat   | `aelf.Ad |             | The address of organization. |
| ion_address | dress <# |             |                              |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| organi      | `a       |             | The organizations id.        |
| zation_hash | elf.Hash |             |                              |
|             |  <#aelf. |             |                              |
|             | Hash>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| cre         | `a       |             | The creation token is for    |
| ation_token | elf.Hash |             | organization address         |
|             |  <#aelf. |             | generation.                  |
|             | Hash>`__ |             |                              |
+-------------+----------+-------------+------------------------------+

Association.OrganizationMemberList
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| organizat   | `aelf.Ad | repeated    | The address of organization  |
| ion_members | dress <# |             | members.                     |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+

Association.ProposalInfo
~~~~~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| proposal_id | `a       |             | The proposal ID.             |
|             | elf.Hash |             |                              |
|             |  <#aelf. |             |                              |
|             | Hash>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| contract_   | `str     |             | The method that this         |
| method_name | ing <#st |             | proposal will call when      |
|             | ring>`__ |             | being released.              |
+-------------+----------+-------------+------------------------------+
| to_address  | `aelf.Ad |             | The address of the target    |
|             | dress <# |             | contract.                    |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| params      | `b       |             | The parameters of the        |
|             | ytes <#b |             | release transaction.         |
|             | ytes>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| e           | `g       |             | The date at which this       |
| xpired_time | oogle.pr |             | proposal will expire.        |
|             | otobuf.T |             |                              |
|             | imestamp |             |                              |
|             |  <#googl |             |                              |
|             | e.protob |             |                              |
|             | uf.Times |             |                              |
|             | tamp>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| proposer    | `aelf.Ad |             | The address of the proposer  |
|             | dress <# |             | of this proposal.            |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| organizat   | `aelf.Ad |             | The address of this          |
| ion_address | dress <# |             | proposals organization.      |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| approvals   | `aelf.Ad | repeated    | Address list of approved.    |
|             | dress <# |             |                              |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| rejections  | `aelf.Ad | repeated    | Address list of rejected.    |
|             | dress <# |             |                              |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| abstentions | `aelf.Ad | repeated    | Address list of abstained.   |
|             | dress <# |             |                              |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| pr          | `str     |             | Url is used for proposal     |
| oposal_desc | ing <#st |             | describing.                  |
| ription_url | ring>`__ |             |                              |
+-------------+----------+-------------+------------------------------+

acs1.MethodFee
~~~~~~~~~~~~~~

========= ==================== ===== ===================================
Field     Type                 Label Description
========= ==================== ===== ===================================
symbol    `string <#string>`__       The token symbol of the method fee.
basic_fee `int64 <#int64>`__         The amount of fees to be charged.
========= ==================== ===== ===================================

acs1.MethodFees
~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| method_name | `str     |             | The name of the method to be |
|             | ing <#st |             | charged.                     |
|             | ring>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| fees        | `Method  | repeated    | List of fees to be charged.  |
|             | Fee <#ac |             |                              |
|             | s1.Metho |             |                              |
|             | dFee>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| is_si       | `bool <# |             | Optional based on the        |
| ze_fee_free | bool>`__ |             | implementation of            |
|             |          |             | SetMethodFee method.         |
+-------------+----------+-------------+------------------------------+

acs3.CreateProposalBySystemContractInput
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-----------------+-------------------+-------+-------------------+
| Field           | Type              | Label | Description       |
+=================+===================+=======+===================+
| proposal_input  | `CreateProposalIn |       | The parameters of |
|                 | put <#acs3.Create |       | creating          |
|                 | ProposalInput>`__ |       | proposal.         |
+-----------------+-------------------+-------+-------------------+
| origin_proposer | `aelf.Address <   |       | The actor that    |
|                 | #aelf.Address>`__ |       | trigger the call. |
+-----------------+-------------------+-------+-------------------+

acs3.CreateProposalInput
~~~~~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| contract_   | `str     |             | The name of the method to    |
| method_name | ing <#st |             | call after release.          |
|             | ring>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| to_address  | `aelf.Ad |             | The address of the contract  |
|             | dress <# |             | to call after release.       |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| params      | `b       |             | The parameter of the method  |
|             | ytes <#b |             | to be called after the       |
|             | ytes>`__ |             | release.                     |
+-------------+----------+-------------+------------------------------+
| e           | `g       |             | The timestamp at which this  |
| xpired_time | oogle.pr |             | proposal will expire.        |
|             | otobuf.T |             |                              |
|             | imestamp |             |                              |
|             |  <#googl |             |                              |
|             | e.protob |             |                              |
|             | uf.Times |             |                              |
|             | tamp>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| organizat   | `aelf.Ad |             | The address of the           |
| ion_address | dress <# |             | organization.                |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| pr          | `str     |             | Url is used for proposal     |
| oposal_desc | ing <#st |             | describing.                  |
| ription_url | ring>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| token       | `a       |             | The token is for proposal id |
|             | elf.Hash |             | generation and with this     |
|             |  <#aelf. |             | token, proposal id can be    |
|             | Hash>`__ |             | calculated before proposing. |
+-------------+----------+-------------+------------------------------+

acs3.OrganizationCreated
~~~~~~~~~~~~~~~~~~~~~~~~

+-------------------+-------------------+-------+-------------------+
| Field             | Type              | Label | Description       |
+===================+===================+=======+===================+
| org               | `aelf.Address <   |       | The address of    |
| anization_address | #aelf.Address>`__ |       | the created       |
|                   |                   |       | organization.     |
+-------------------+-------------------+-------+-------------------+

acs3.OrganizationHashAddressPair
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------------+-------------------+-------+-------------------+
| Field             | Type              | Label | Description       |
+===================+===================+=======+===================+
| organization_hash | `aelf.Has         |       | The id of         |
|                   | h <#aelf.Hash>`__ |       | organization.     |
+-------------------+-------------------+-------+-------------------+
| org               | `aelf.Address <   |       | The address of    |
| anization_address | #aelf.Address>`__ |       | organization.     |
+-------------------+-------------------+-------+-------------------+

acs3.OrganizationThresholdChanged
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| organizat   | `aelf.Ad |             | The organization address     |
| ion_address | dress <# |             |                              |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| prop        | `Prop    |             | The new release threshold.   |
| oser_releas | osalRele |             |                              |
| e_threshold | aseThres |             |                              |
|             | hold <#a |             |                              |
|             | cs3.Prop |             |                              |
|             | osalRele |             |                              |
|             | aseThres |             |                              |
|             | hold>`__ |             |                              |
+-------------+----------+-------------+------------------------------+

acs3.OrganizationWhiteListChanged
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------------+-------------------+-------+-------------------+
| Field             | Type              | Label | Description       |
+===================+===================+=======+===================+
| org               | `aelf.Address <   |       | The organization  |
| anization_address | #aelf.Address>`__ |       | address.          |
+-------------------+-------------------+-------+-------------------+
| pr                | `ProposerWhit     |       | The new proposer  |
| oposer_white_list | eList <#acs3.Prop |       | whitelist.        |
|                   | oserWhiteList>`__ |       |                   |
+-------------------+-------------------+-------+-------------------+

acs3.ProposalCreated
~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| proposal_id | `a       |             | The id of the created        |
|             | elf.Hash |             | proposal.                    |
|             |  <#aelf. |             |                              |
|             | Hash>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| organizat   | `aelf.Ad |             | The organization address of  |
| ion_address | dress <# |             | the created proposal.        |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+

acs3.ProposalOutput
~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| proposal_id | `a       |             | The id of the proposal.      |
|             | elf.Hash |             |                              |
|             |  <#aelf. |             |                              |
|             | Hash>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| contract_   | `str     |             | The method that this         |
| method_name | ing <#st |             | proposal will call when      |
|             | ring>`__ |             | being released.              |
+-------------+----------+-------------+------------------------------+
| to_address  | `aelf.Ad |             | The address of the target    |
|             | dress <# |             | contract.                    |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| params      | `b       |             | The parameters of the        |
|             | ytes <#b |             | release transaction.         |
|             | ytes>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| e           | `g       |             | The date at which this       |
| xpired_time | oogle.pr |             | proposal will expire.        |
|             | otobuf.T |             |                              |
|             | imestamp |             |                              |
|             |  <#googl |             |                              |
|             | e.protob |             |                              |
|             | uf.Times |             |                              |
|             | tamp>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| organizat   | `aelf.Ad |             | The address of this          |
| ion_address | dress <# |             | proposals organization.      |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| proposer    | `aelf.Ad |             | The address of the proposer  |
|             | dress <# |             | of this proposal.            |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| to_         | `bool <# |             | Indicates if this proposal   |
| be_released | bool>`__ |             | is releasable.               |
+-------------+----------+-------------+------------------------------+
| app         | `i       |             | Approval count for this      |
| roval_count | nt64 <#i |             | proposal.                    |
|             | nt64>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| reje        | `i       |             | Rejection count for this     |
| ction_count | nt64 <#i |             | proposal.                    |
|             | nt64>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| abste       | `i       |             | Abstention count for this    |
| ntion_count | nt64 <#i |             | proposal.                    |
|             | nt64>`__ |             |                              |
+-------------+----------+-------------+------------------------------+

acs3.ProposalReleaseThreshold
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| mini        | `i       |             | The value for the minimum    |
| mal_approva | nt64 <#i |             | approval threshold.          |
| l_threshold | nt64>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| maxim       | `i       |             | The value for the maximal    |
| al_rejectio | nt64 <#i |             | rejection threshold.         |
| n_threshold | nt64>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| maxima      | `i       |             | The value for the maximal    |
| l_abstentio | nt64 <#i |             | abstention threshold.        |
| n_threshold | nt64>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| minimal_vot | `i       |             | The value for the minimal    |
| e_threshold | nt64 <#i |             | vote threshold.              |
|             | nt64>`__ |             |                              |
+-------------+----------+-------------+------------------------------+

acs3.ProposalReleased
~~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| proposal_id | `a       |             | The id of the released       |
|             | elf.Hash |             | proposal.                    |
|             |  <#aelf. |             |                              |
|             | Hash>`__ |             |                              |
+-------------+----------+-------------+------------------------------+
| organizat   | `aelf.Ad |             | The organization address of  |
| ion_address | dress <# |             | the released proposal.       |
|             | aelf.Add |             |                              |
|             | ress>`__ |             |                              |
+-------------+----------+-------------+------------------------------+

acs3.ProposerWhiteList
~~~~~~~~~~~~~~~~~~~~~~

+-----------+---------------------+----------+---------------------+
| Field     | Type                | Label    | Description         |
+===========+=====================+==========+=====================+
| proposers | `aelf.Address       | repeated | The address of the  |
|           |  <#aelf.Address>`__ |          | proposers           |
+-----------+---------------------+----------+---------------------+

acs3.ReceiptCreated
~~~~~~~~~~~~~~~~~~~

+-------------------+-------------------+-------+-------------------+
| Field             | Type              | Label | Description       |
+===================+===================+=======+===================+
| proposal_id       | `aelf.Has         |       | The id of the     |
|                   | h <#aelf.Hash>`__ |       | proposal.         |
+-------------------+-------------------+-------+-------------------+
| address           | `aelf.Address <   |       | The sender        |
|                   | #aelf.Address>`__ |       | address.          |
+-------------------+-------------------+-------+-------------------+
| receipt_type      | `st               |       | The type of       |
|                   | ring <#string>`__ |       | receipt(Approve,  |
|                   |                   |       | Reject or         |
|                   |                   |       | Abstain).         |
+-------------------+-------------------+-------+-------------------+
| time              | `google           |       | The timestamp of  |
|                   | .protobuf.Timesta |       | this method call. |
|                   | mp <#google.proto |       |                   |
|                   | buf.Timestamp>`__ |       |                   |
+-------------------+-------------------+-------+-------------------+
| org               | `aelf.Address <   |       | The address of    |
| anization_address | #aelf.Address>`__ |       | the organization. |
+-------------------+-------------------+-------+-------------------+

acs3.ValidateProposerInWhiteListInput
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------------+-------------------+-------+-------------------+
| Field             | Type              | Label | Description       |
+===================+===================+=======+===================+
| proposer          | `aelf.Address <   |       | The address to    |
|                   | #aelf.Address>`__ |       | search/check.     |
+-------------------+-------------------+-------+-------------------+
| org               | `aelf.Address <   |       | The address of    |
| anization_address | #aelf.Address>`__ |       | the organization. |
+-------------------+-------------------+-------+-------------------+

.AuthorityInfo
~~~~~~~~~~~~~~

+------------------+-------------------+-------+-------------------+
| Field            | Type              | Label | Description       |
+==================+===================+=======+===================+
| contract_address | `aelf.Address <   |       | The contract      |
|                  | #aelf.Address>`__ |       | address of the    |
|                  |                   |       | controller.       |
+------------------+-------------------+-------+-------------------+
| owner_address    | `aelf.Address <   |       | The address of    |
|                  | #aelf.Address>`__ |       | the owner of the  |
|                  |                   |       | contract.         |
+------------------+-------------------+-------+-------------------+

aelf.Address
~~~~~~~~~~~~

===== ================== ===== ===========
Field Type               Label Description
===== ================== ===== ===========
value `bytes <#bytes>`__       
===== ================== ===== ===========

aelf.BinaryMerkleTree
~~~~~~~~~~~~~~~~~~~~~

========== ===================== ======== ===========
Field      Type                  Label    Description
========== ===================== ======== ===========
nodes      `Hash <#aelf.Hash>`__ repeated 
root       `Hash <#aelf.Hash>`__          
leaf_count `int32 <#int32>`__             
========== ===================== ======== ===========

aelf.Hash
~~~~~~~~~

===== ================== ===== ===========
Field Type               Label Description
===== ================== ===== ===========
value `bytes <#bytes>`__       
===== ================== ===== ===========

aelf.LogEvent
~~~~~~~~~~~~~

=========== =========================== ======== ===========
Field       Type                        Label    Description
=========== =========================== ======== ===========
address     `Address <#aelf.Address>`__          
name        `string <#string>`__                 
indexed     `bytes <#bytes>`__          repeated 
non_indexed `bytes <#bytes>`__                   
=========== =========================== ======== ===========

aelf.MerklePath
~~~~~~~~~~~~~~~

+-------------------+--------------------+----------+-------------+
| Field             | Type               | Label    | Description |
+===================+====================+==========+=============+
| merkle_path_nodes | `Merk              | repeated |             |
|                   | lePathNode <#aelf. |          |             |
|                   | MerklePathNode>`__ |          |             |
+-------------------+--------------------+----------+-------------+

aelf.MerklePathNode
~~~~~~~~~~~~~~~~~~~

================== ===================== ===== ===========
Field              Type                  Label Description
================== ===================== ===== ===========
hash               `Hash <#aelf.Hash>`__       
is_left_child_node `bool <#bool>`__            
================== ===================== ===== ===========

aelf.SInt32Value
~~~~~~~~~~~~~~~~

===== ==================== ===== ===========
Field Type                 Label Description
===== ==================== ===== ===========
value `sint32 <#sint32>`__       
===== ==================== ===== ===========

aelf.SInt64Value
~~~~~~~~~~~~~~~~

===== ==================== ===== ===========
Field Type                 Label Description
===== ==================== ===== ===========
value `sint64 <#sint64>`__       
===== ==================== ===== ===========

aelf.ScopedStatePath
~~~~~~~~~~~~~~~~~~~~

======= =============================== ===== ===========
Field   Type                            Label Description
======= =============================== ===== ===========
address `Address <#aelf.Address>`__           
path    `StatePath <#aelf.StatePath>`__       
======= =============================== ===== ===========

aelf.SmartContractRegistration
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

================== ===================== ===== ===========
Field              Type                  Label Description
================== ===================== ===== ===========
category           `sint32 <#sint32>`__        
code               `bytes <#bytes>`__          
code_hash          `Hash <#aelf.Hash>`__       
is_system_contract `bool <#bool>`__            
version            `int32 <#int32>`__          
================== ===================== ===== ===========

aelf.StatePath
~~~~~~~~~~~~~~

===== ==================== ======== ===========
Field Type                 Label    Description
===== ==================== ======== ===========
parts `string <#string>`__ repeated 
===== ==================== ======== ===========

aelf.Transaction
~~~~~~~~~~~~~~~~

================ =========================== ===== ===========
Field            Type                        Label Description
================ =========================== ===== ===========
from             `Address <#aelf.Address>`__       
to               `Address <#aelf.Address>`__       
ref_block_number `int64 <#int64>`__                
ref_block_prefix `bytes <#bytes>`__                
method_name      `string <#string>`__              
params           `bytes <#bytes>`__                
signature        `bytes <#bytes>`__                
================ =========================== ===== ===========

aelf.TransactionExecutingStateSet
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+---------+--------------------------------+----------+-------------+
| Field   | Type                           | Label    | Description |
+=========+================================+==========+=============+
| writes  | `Tr                            | repeated |             |
|         | ansactionExecutingStateSet.Wri |          |             |
|         | tesEntry <#aelf.TransactionExe |          |             |
|         | cutingStateSet.WritesEntry>`__ |          |             |
+---------+--------------------------------+----------+-------------+
| reads   | `                              | repeated |             |
|         | TransactionExecutingStateSet.R |          |             |
|         | eadsEntry <#aelf.TransactionEx |          |             |
|         | ecutingStateSet.ReadsEntry>`__ |          |             |
+---------+--------------------------------+----------+-------------+
| deletes | `Tran                          | repeated |             |
|         | sactionExecutingStateSet.Delet |          |             |
|         | esEntry <#aelf.TransactionExec |          |             |
|         | utingStateSet.DeletesEntry>`__ |          |             |
+---------+--------------------------------+----------+-------------+

aelf.TransactionExecutingStateSet.DeletesEntry
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

===== ==================== ===== ===========
Field Type                 Label Description
===== ==================== ===== ===========
key   `string <#string>`__       
value `bool <#bool>`__           
===== ==================== ===== ===========

aelf.TransactionExecutingStateSet.ReadsEntry
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

===== ==================== ===== ===========
Field Type                 Label Description
===== ==================== ===== ===========
key   `string <#string>`__       
value `bool <#bool>`__           
===== ==================== ===== ===========

aelf.TransactionExecutingStateSet.WritesEntry
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

===== ==================== ===== ===========
Field Type                 Label Description
===== ==================== ===== ===========
key   `string <#string>`__       
value `bytes <#bytes>`__         
===== ==================== ===== ===========

aelf.TransactionResult
~~~~~~~~~~~~~~~~~~~~~~

+----------------+--------------------+----------+-------------+
| Field          | Type               | Label    | Description |
+================+====================+==========+=============+
| transaction_id | `Ha                |          |             |
|                | sh <#aelf.Hash>`__ |          |             |
+----------------+--------------------+----------+-------------+
| status         | `Tran              |          |             |
|                | sactionResultStatu |          |             |
|                | s <#aelf.Transacti |          |             |
|                | onResultStatus>`__ |          |             |
+----------------+--------------------+----------+-------------+
| logs           | `LogEvent <        | repeated |             |
|                | #aelf.LogEvent>`__ |          |             |
+----------------+--------------------+----------+-------------+
| bloom          | `bytes <#bytes>`__ |          |             |
+----------------+--------------------+----------+-------------+
| return_value   | `bytes <#bytes>`__ |          |             |
+----------------+--------------------+----------+-------------+
| block_number   | `int64 <#int64>`__ |          |             |
+----------------+--------------------+----------+-------------+
| block_hash     | `Ha                |          |             |
|                | sh <#aelf.Hash>`__ |          |             |
+----------------+--------------------+----------+-------------+
| error          | `s                 |          |             |
|                | tring <#string>`__ |          |             |
+----------------+--------------------+----------+-------------+

aelf.TransactionResultStatus
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

====================== ====== ===========
Name                   Number Description
====================== ====== ===========
NOT_EXISTED            0      
PENDING                1      
FAILED                 2      
MINED                  3      
CONFLICT               4      
PENDING_VALIDATION     5      
NODE_VALIDATION_FAILED 6      
====================== ====== ===========

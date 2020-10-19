


<a name="association_contract_impl.proto"></a>

## AElf.Contracts.Association
Association contract.

Organizations established to achieve specific goals 
can use this contract to cooperatively handle transactions within the organization

Implement AElf Standards ACS1 and ACS3.

| Method Name | Request Type | Response Type | Description |
| ----------- | ------------ | ------------- | ------------|
| CreateOrganization | [CreateOrganizationInput](#Association.CreateOrganizationInput) | [.aelf.Address](#aelf.Address) | Create an organization and return its address. |
| CreateOrganizationBySystemContract | [CreateOrganizationBySystemContractInput](#Association.CreateOrganizationBySystemContractInput) | [.aelf.Address](#aelf.Address) | Creates an organization by system contract and return its address. |
| AddMember | [.aelf.Address](#aelf.Address) | [.google.protobuf.Empty](#google.protobuf.Empty) | Add organization members. |
| RemoveMember | [.aelf.Address](#aelf.Address) | [.google.protobuf.Empty](#google.protobuf.Empty) | Remove organization members. |
| ChangeMember | [ChangeMemberInput](#Association.ChangeMemberInput) | [.google.protobuf.Empty](#google.protobuf.Empty) | Replace organization member with a new member. |
| GetOrganization | [.aelf.Address](#aelf.Address) | [Organization](#Association.Organization) | Get the organization according to the organization address. |
| CalculateOrganizationAddress | [CreateOrganizationInput](#Association.CreateOrganizationInput) | [.aelf.Address](#aelf.Address) | Calculate the input and return the organization address. |
| SetMethodFee | [MethodFees](#acs1.MethodFees) | [.google.protobuf.Empty](#google.protobuf.Empty) | Set the method fees for the specified method. Note that this will override all fees of the method. |
| ChangeMethodFeeController | [.AuthorityInfo](#AuthorityInfo) | [.google.protobuf.Empty](#google.protobuf.Empty) | Change the method fee controller, the default is parliament and default organization. |
| GetMethodFee | [.google.protobuf.StringValue](#google.protobuf.StringValue) | [MethodFees](#acs1.MethodFees) | Query method fee information by method name. |
| GetMethodFeeController | [.google.protobuf.Empty](#google.protobuf.Empty) | [.AuthorityInfo](#AuthorityInfo) | Query the method fee controller. |
| CreateProposal | [CreateProposalInput](#acs3.CreateProposalInput) | [.aelf.Hash](#aelf.Hash) | Create a proposal for which organization members can vote. When the proposal is released, a transaction will be sent to the specified contract. Return id of the newly created proposal. |
| Approve | [.aelf.Hash](#aelf.Hash) | [.google.protobuf.Empty](#google.protobuf.Empty) | Approve a proposal according to the proposal ID. |
| Reject | [.aelf.Hash](#aelf.Hash) | [.google.protobuf.Empty](#google.protobuf.Empty) | Reject a proposal according to the proposal ID. |
| Abstain | [.aelf.Hash](#aelf.Hash) | [.google.protobuf.Empty](#google.protobuf.Empty) | Abstain a proposal according to the proposal ID. |
| Release | [.aelf.Hash](#aelf.Hash) | [.google.protobuf.Empty](#google.protobuf.Empty) | Release a proposal according to the proposal ID and send a transaction to the specified contract. |
| ChangeOrganizationThreshold | [ProposalReleaseThreshold](#acs3.ProposalReleaseThreshold) | [.google.protobuf.Empty](#google.protobuf.Empty) | Change the thresholds associated with proposals. All fields will be overwritten by the input value and this will affect all current proposals of the organization. Note: only the organization can execute this through a proposal. |
| ChangeOrganizationProposerWhiteList | [ProposerWhiteList](#acs3.ProposerWhiteList) | [.google.protobuf.Empty](#google.protobuf.Empty) | Change the white list of organization proposer. This method overrides the list of whitelisted proposers. |
| CreateProposalBySystemContract | [CreateProposalBySystemContractInput](#acs3.CreateProposalBySystemContractInput) | [.aelf.Hash](#aelf.Hash) | Create a proposal by system contracts, and return id of the newly created proposal. |
| ClearProposal | [.aelf.Hash](#aelf.Hash) | [.google.protobuf.Empty](#google.protobuf.Empty) | Remove the specified proposal. If the proposal is in effect, the cleanup fails. |
| GetProposal | [.aelf.Hash](#aelf.Hash) | [ProposalOutput](#acs3.ProposalOutput) | Get the proposal according to the proposal ID. |
| ValidateOrganizationExist | [.aelf.Address](#aelf.Address) | [.google.protobuf.BoolValue](#google.protobuf.BoolValue) | Check the existence of an organization. |
| ValidateProposerInWhiteList | [ValidateProposerInWhiteListInput](#acs3.ValidateProposerInWhiteListInput) | [.google.protobuf.BoolValue](#google.protobuf.BoolValue) | Check if the proposer is whitelisted. |










 <!-- end messages -->

 <!-- end enums -->

 <!-- end HasExtensions -->




<div id="Association.ChangeMemberInput"></div>

### Association.ChangeMemberInput



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| old_member | [aelf.Address](#aelf.Address) |  | The old member address. |
| new_member | [aelf.Address](#aelf.Address) |  | The new member address. |






<div id="Association.CreateOrganizationBySystemContractInput"></div>

### Association.CreateOrganizationBySystemContractInput



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| organization_creation_input | [CreateOrganizationInput](#Association.CreateOrganizationInput) |  | The parameters of creating organization. |
| organization_address_feedback_method | [string](#string) |  | The organization address callback method which replies the organization address to caller contract. |






<div id="Association.CreateOrganizationInput"></div>

### Association.CreateOrganizationInput



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| organization_member_list | [OrganizationMemberList](#Association.OrganizationMemberList) |  | Initial organization members. |
| proposal_release_threshold | [acs3.ProposalReleaseThreshold](#acs3.ProposalReleaseThreshold) |  | The threshold for releasing the proposal. |
| proposer_white_list | [acs3.ProposerWhiteList](#acs3.ProposerWhiteList) |  | The proposer whitelist. |
| creation_token | [aelf.Hash](#aelf.Hash) |  | The creation token is for organization address generation. |






<div id="Association.MemberAdded"></div>

### Association.MemberAdded



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| member | [aelf.Address](#aelf.Address) |  | The added member address. |
| organization_address | [aelf.Address](#aelf.Address) |  | The organization address. |






<div id="Association.MemberChanged"></div>

### Association.MemberChanged



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| old_member | [aelf.Address](#aelf.Address) |  | The old member address. |
| new_member | [aelf.Address](#aelf.Address) |  | The new member address. |
| organization_address | [aelf.Address](#aelf.Address) |  | The organization address. |






<div id="Association.MemberRemoved"></div>

### Association.MemberRemoved



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| member | [aelf.Address](#aelf.Address) |  | The removed member address. |
| organization_address | [aelf.Address](#aelf.Address) |  | The organization address. |






<div id="Association.Organization"></div>

### Association.Organization



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| organization_member_list | [OrganizationMemberList](#Association.OrganizationMemberList) |  | The organization members. |
| proposal_release_threshold | [acs3.ProposalReleaseThreshold](#acs3.ProposalReleaseThreshold) |  | The threshold for releasing the proposal. |
| proposer_white_list | [acs3.ProposerWhiteList](#acs3.ProposerWhiteList) |  | The proposer whitelist. |
| organization_address | [aelf.Address](#aelf.Address) |  | The address of organization. |
| organization_hash | [aelf.Hash](#aelf.Hash) |  | The organizations id. |
| creation_token | [aelf.Hash](#aelf.Hash) |  | The creation token is for organization address generation. |






<div id="Association.OrganizationMemberList"></div>

### Association.OrganizationMemberList



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| organization_members | [aelf.Address](#aelf.Address) | repeated | The address of organization members. |






<div id="Association.ProposalInfo"></div>

### Association.ProposalInfo



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| proposal_id | [aelf.Hash](#aelf.Hash) |  | The proposal ID. |
| contract_method_name | [string](#string) |  | The method that this proposal will call when being released. |
| to_address | [aelf.Address](#aelf.Address) |  | The address of the target contract. |
| params | [bytes](#bytes) |  | The parameters of the release transaction. |
| expired_time | [google.protobuf.Timestamp](#google.protobuf.Timestamp) |  | The date at which this proposal will expire. |
| proposer | [aelf.Address](#aelf.Address) |  | The address of the proposer of this proposal. |
| organization_address | [aelf.Address](#aelf.Address) |  | The address of this proposals organization. |
| approvals | [aelf.Address](#aelf.Address) | repeated | Address list of approved. |
| rejections | [aelf.Address](#aelf.Address) | repeated | Address list of rejected. |
| abstentions | [aelf.Address](#aelf.Address) | repeated | Address list of abstained. |
| proposal_description_url | [string](#string) |  | Url is used for proposal describing. |





 <!-- end messages -->

 <!-- end enums -->

 <!-- end HasExtensions -->




<div id="acs1.MethodFee"></div>

### acs1.MethodFee



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| symbol | [string](#string) |  | The token symbol of the method fee. |
| basic_fee | [int64](#int64) |  | The amount of fees to be charged. |






<div id="acs1.MethodFees"></div>

### acs1.MethodFees



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| method_name | [string](#string) |  | The name of the method to be charged. |
| fees | [MethodFee](#acs1.MethodFee) | repeated | List of fees to be charged. |
| is_size_fee_free | [bool](#bool) |  | Optional based on the implementation of SetMethodFee method. |





 <!-- end messages -->

 <!-- end enums -->

 <!-- end HasExtensions -->




<div id="acs3.CreateProposalBySystemContractInput"></div>

### acs3.CreateProposalBySystemContractInput



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| proposal_input | [CreateProposalInput](#acs3.CreateProposalInput) |  | The parameters of creating proposal. |
| origin_proposer | [aelf.Address](#aelf.Address) |  | The actor that trigger the call. |






<div id="acs3.CreateProposalInput"></div>

### acs3.CreateProposalInput



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| contract_method_name | [string](#string) |  | The name of the method to call after release. |
| to_address | [aelf.Address](#aelf.Address) |  | The address of the contract to call after release. |
| params | [bytes](#bytes) |  | The parameter of the method to be called after the release. |
| expired_time | [google.protobuf.Timestamp](#google.protobuf.Timestamp) |  | The timestamp at which this proposal will expire. |
| organization_address | [aelf.Address](#aelf.Address) |  | The address of the organization. |
| proposal_description_url | [string](#string) |  | Url is used for proposal describing. |
| token | [aelf.Hash](#aelf.Hash) |  | The token is for proposal id generation and with this token, proposal id can be calculated before proposing. |






<div id="acs3.OrganizationCreated"></div>

### acs3.OrganizationCreated



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| organization_address | [aelf.Address](#aelf.Address) |  | The address of the created organization. |






<div id="acs3.OrganizationHashAddressPair"></div>

### acs3.OrganizationHashAddressPair



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| organization_hash | [aelf.Hash](#aelf.Hash) |  | The id of organization. |
| organization_address | [aelf.Address](#aelf.Address) |  | The address of organization. |






<div id="acs3.OrganizationThresholdChanged"></div>

### acs3.OrganizationThresholdChanged



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| organization_address | [aelf.Address](#aelf.Address) |  | The organization address |
| proposer_release_threshold | [ProposalReleaseThreshold](#acs3.ProposalReleaseThreshold) |  | The new release threshold. |






<div id="acs3.OrganizationWhiteListChanged"></div>

### acs3.OrganizationWhiteListChanged



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| organization_address | [aelf.Address](#aelf.Address) |  | The organization address. |
| proposer_white_list | [ProposerWhiteList](#acs3.ProposerWhiteList) |  | The new proposer whitelist. |






<div id="acs3.ProposalCreated"></div>

### acs3.ProposalCreated



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| proposal_id | [aelf.Hash](#aelf.Hash) |  | The id of the created proposal. |
| organization_address | [aelf.Address](#aelf.Address) |  | The organization address of the created proposal. |






<div id="acs3.ProposalOutput"></div>

### acs3.ProposalOutput



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| proposal_id | [aelf.Hash](#aelf.Hash) |  | The id of the proposal. |
| contract_method_name | [string](#string) |  | The method that this proposal will call when being released. |
| to_address | [aelf.Address](#aelf.Address) |  | The address of the target contract. |
| params | [bytes](#bytes) |  | The parameters of the release transaction. |
| expired_time | [google.protobuf.Timestamp](#google.protobuf.Timestamp) |  | The date at which this proposal will expire. |
| organization_address | [aelf.Address](#aelf.Address) |  | The address of this proposals organization. |
| proposer | [aelf.Address](#aelf.Address) |  | The address of the proposer of this proposal. |
| to_be_released | [bool](#bool) |  | Indicates if this proposal is releasable. |
| approval_count | [int64](#int64) |  | Approval count for this proposal. |
| rejection_count | [int64](#int64) |  | Rejection count for this proposal. |
| abstention_count | [int64](#int64) |  | Abstention count for this proposal. |






<div id="acs3.ProposalReleaseThreshold"></div>

### acs3.ProposalReleaseThreshold



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| minimal_approval_threshold | [int64](#int64) |  | The value for the minimum approval threshold. |
| maximal_rejection_threshold | [int64](#int64) |  | The value for the maximal rejection threshold. |
| maximal_abstention_threshold | [int64](#int64) |  | The value for the maximal abstention threshold. |
| minimal_vote_threshold | [int64](#int64) |  | The value for the minimal vote threshold. |






<div id="acs3.ProposalReleased"></div>

### acs3.ProposalReleased



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| proposal_id | [aelf.Hash](#aelf.Hash) |  | The id of the released proposal. |
| organization_address | [aelf.Address](#aelf.Address) |  | The organization address of the released proposal. |






<div id="acs3.ProposerWhiteList"></div>

### acs3.ProposerWhiteList



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| proposers | [aelf.Address](#aelf.Address) | repeated | The address of the proposers |






<div id="acs3.ReceiptCreated"></div>

### acs3.ReceiptCreated



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| proposal_id | [aelf.Hash](#aelf.Hash) |  | The id of the proposal. |
| address | [aelf.Address](#aelf.Address) |  | The sender address. |
| receipt_type | [string](#string) |  | The type of receipt(Approve, Reject or Abstain). |
| time | [google.protobuf.Timestamp](#google.protobuf.Timestamp) |  | The timestamp of this method call. |
| organization_address | [aelf.Address](#aelf.Address) |  | The address of the organization. |






<div id="acs3.ValidateProposerInWhiteListInput"></div>

### acs3.ValidateProposerInWhiteListInput



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| proposer | [aelf.Address](#aelf.Address) |  | The address to search/check. |
| organization_address | [aelf.Address](#aelf.Address) |  | The address of the organization. |





 <!-- end messages -->

 <!-- end enums -->

 <!-- end HasExtensions -->




<div id=".AuthorityInfo"></div>

### .AuthorityInfo



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| contract_address | [aelf.Address](#aelf.Address) |  | The contract address of the controller. |
| owner_address | [aelf.Address](#aelf.Address) |  | The address of the owner of the contract. |





 <!-- end messages -->

 <!-- end enums -->

 <!-- end HasExtensions -->




<div id="aelf.Address"></div>

### aelf.Address



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| value | [bytes](#bytes) |  |  |






<div id="aelf.BinaryMerkleTree"></div>

### aelf.BinaryMerkleTree



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| nodes | [Hash](#aelf.Hash) | repeated |  |
| root | [Hash](#aelf.Hash) |  |  |
| leaf_count | [int32](#int32) |  |  |






<div id="aelf.Hash"></div>

### aelf.Hash



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| value | [bytes](#bytes) |  |  |






<div id="aelf.LogEvent"></div>

### aelf.LogEvent



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| address | [Address](#aelf.Address) |  |  |
| name | [string](#string) |  |  |
| indexed | [bytes](#bytes) | repeated |  |
| non_indexed | [bytes](#bytes) |  |  |






<div id="aelf.MerklePath"></div>

### aelf.MerklePath



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| merkle_path_nodes | [MerklePathNode](#aelf.MerklePathNode) | repeated |  |






<div id="aelf.MerklePathNode"></div>

### aelf.MerklePathNode



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| hash | [Hash](#aelf.Hash) |  |  |
| is_left_child_node | [bool](#bool) |  |  |






<div id="aelf.SInt32Value"></div>

### aelf.SInt32Value



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| value | [sint32](#sint32) |  |  |






<div id="aelf.SInt64Value"></div>

### aelf.SInt64Value



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| value | [sint64](#sint64) |  |  |






<div id="aelf.ScopedStatePath"></div>

### aelf.ScopedStatePath



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| address | [Address](#aelf.Address) |  |  |
| path | [StatePath](#aelf.StatePath) |  |  |






<div id="aelf.SmartContractRegistration"></div>

### aelf.SmartContractRegistration



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| category | [sint32](#sint32) |  |  |
| code | [bytes](#bytes) |  |  |
| code_hash | [Hash](#aelf.Hash) |  |  |
| is_system_contract | [bool](#bool) |  |  |
| version | [int32](#int32) |  |  |






<div id="aelf.StatePath"></div>

### aelf.StatePath



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| parts | [string](#string) | repeated |  |






<div id="aelf.Transaction"></div>

### aelf.Transaction



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| from | [Address](#aelf.Address) |  |  |
| to | [Address](#aelf.Address) |  |  |
| ref_block_number | [int64](#int64) |  |  |
| ref_block_prefix | [bytes](#bytes) |  |  |
| method_name | [string](#string) |  |  |
| params | [bytes](#bytes) |  |  |
| signature | [bytes](#bytes) |  |  |






<div id="aelf.TransactionExecutingStateSet"></div>

### aelf.TransactionExecutingStateSet



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| writes | [TransactionExecutingStateSet.WritesEntry](#aelf.TransactionExecutingStateSet.WritesEntry) | repeated |  |
| reads | [TransactionExecutingStateSet.ReadsEntry](#aelf.TransactionExecutingStateSet.ReadsEntry) | repeated |  |
| deletes | [TransactionExecutingStateSet.DeletesEntry](#aelf.TransactionExecutingStateSet.DeletesEntry) | repeated |  |






<div id="aelf.TransactionExecutingStateSet.DeletesEntry"></div>

### aelf.TransactionExecutingStateSet.DeletesEntry



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| key | [string](#string) |  |  |
| value | [bool](#bool) |  |  |






<div id="aelf.TransactionExecutingStateSet.ReadsEntry"></div>

### aelf.TransactionExecutingStateSet.ReadsEntry



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| key | [string](#string) |  |  |
| value | [bool](#bool) |  |  |






<div id="aelf.TransactionExecutingStateSet.WritesEntry"></div>

### aelf.TransactionExecutingStateSet.WritesEntry



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| key | [string](#string) |  |  |
| value | [bytes](#bytes) |  |  |






<div id="aelf.TransactionResult"></div>

### aelf.TransactionResult



| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| transaction_id | [Hash](#aelf.Hash) |  |  |
| status | [TransactionResultStatus](#aelf.TransactionResultStatus) |  |  |
| logs | [LogEvent](#aelf.LogEvent) | repeated |  |
| bloom | [bytes](#bytes) |  |  |
| return_value | [bytes](#bytes) |  |  |
| block_number | [int64](#int64) |  |  |
| block_hash | [Hash](#aelf.Hash) |  |  |
| error | [string](#string) |  |  |





 <!-- end messages -->


<div id="aelf.TransactionResultStatus"></div>

### aelf.TransactionResultStatus


| Name | Number | Description |
| ---- | ------ | ----------- |
| NOT_EXISTED | 0 |  |
| PENDING | 1 |  |
| FAILED | 2 |  |
| MINED | 3 |  |
| CONFLICT | 4 |  |
| PENDING_VALIDATION | 5 |  |
| NODE_VALIDATION_FAILED | 6 |  |


 <!-- end enums -->

 <!-- end HasExtensions -->




## Scalar Value Types

| .proto Type | Notes | C++ | Java | Python | Go | C# | PHP | Ruby |
| ----------- | ----- | --- | ---- | ------ | -- | -- | --- | ---- |
| <div id="double" /> double |  | double | double | float | float64 | double | float | Float |
| <div id="float" /> float |  | float | float | float | float32 | float | float | Float |
| <div id="int32" /> int32 | Uses variable-length encoding. Inefficient for encoding negative numbers – if your field is likely to have negative values, use sint32 instead. | int32 | int | int | int32 | int | integer | Bignum or Fixnum (as required) |
| <div id="int64" /> int64 | Uses variable-length encoding. Inefficient for encoding negative numbers – if your field is likely to have negative values, use sint64 instead. | int64 | long | int/long | int64 | long | integer/string | Bignum |
| <div id="uint32" /> uint32 | Uses variable-length encoding. | uint32 | int | int/long | uint32 | uint | integer | Bignum or Fixnum (as required) |
| <div id="uint64" /> uint64 | Uses variable-length encoding. | uint64 | long | int/long | uint64 | ulong | integer/string | Bignum or Fixnum (as required) |
| <div id="sint32" /> sint32 | Uses variable-length encoding. Signed int value. These more efficiently encode negative numbers than regular int32s. | int32 | int | int | int32 | int | integer | Bignum or Fixnum (as required) |
| <div id="sint64" /> sint64 | Uses variable-length encoding. Signed int value. These more efficiently encode negative numbers than regular int64s. | int64 | long | int/long | int64 | long | integer/string | Bignum |
| <div id="fixed32" /> fixed32 | Always four bytes. More efficient than uint32 if values are often greater than 2^28. | uint32 | int | int | uint32 | uint | integer | Bignum or Fixnum (as required) |
| <div id="fixed64" /> fixed64 | Always eight bytes. More efficient than uint64 if values are often greater than 2^56. | uint64 | long | int/long | uint64 | ulong | integer/string | Bignum |
| <div id="sfixed32" /> sfixed32 | Always four bytes. | int32 | int | int | int32 | int | integer | Bignum or Fixnum (as required) |
| <div id="sfixed64" /> sfixed64 | Always eight bytes. | int64 | long | int/long | int64 | long | integer/string | Bignum |
| <div id="bool" /> bool |  | bool | boolean | boolean | bool | bool | boolean | TrueClass/FalseClass |
| <div id="string" /> string | A string must always contain UTF-8 encoded or 7-bit ASCII text. | string | String | str/unicode | string | string | string | String (UTF-8) |
| <div id="bytes" /> bytes | May contain any arbitrary sequence of bytes. | string | ByteString | str | []byte | ByteString | string | String (ASCII-8BIT) |


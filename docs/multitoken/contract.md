## Multitoken contract 

### Creation and issuance

AElf comes with a predefined (system) contract for creating tokens. It's used for creating the native token of a chain, as well as for users of the blockchain to create their own tokens.

To create a token, a transaction must be issued to the **Create** method of the token contract. With the following argument (defined as proto):

``` Proto
message CreateInput {
    string symbol = 1;
    string tokenName = 2;
    sint64 totalSupply = 3;
    sint32 decimals = 4;
}
``` 

You must define the symbol your token will use as well as a more user friendly name. The total supply and the precision of the token.

To issue some tokens, a transaction must be issued to the **Issue** method of the token contract. With the following argument (defined as proto):

``` Proto
message IssueInput {
    string symbol = 1;
    sint64 amount = 2;
    string memo = 3;
    Address to = 4;
}
``` 

This is straight forward, the symbol identifies the token and the **to** the receiver of the new tokens. Note that the amount should not exceed the total supply. After this call the balance of the **to** address will be updated.

Transfer from one address to another are also possible with **Transfer** (TransferInput) and **TransferFrom** (TransferFromInput). **TransferFrom** is explained in the **Allowances** section.

Concerning the **Transfer** method, it will transfer a certain amount of tokens from the senders address to destination address (to).

``` Proto
message TransferInput {
    Address to = 1;
    string symbol = 2;
    sint64 amount = 3;
    string memo = 4;
}
```

it

### Allowances

Allowances allow some entity (in fact an address in this case) to authorize another address to transfer tokens on his behalf. There are two methods available for controlling this, namely **Approve** and **UnApprove**, that take as input respectively, a ApproveInput and UnApproveInput message (both define the same fields).

``` Proto
message ApproveInput/UnApprove {
    Address spender = 1;
    string symbol = 2;
    sint64 amount = 3;
}
```

Allowances are defined as the 4 following values: { approver, approvee, token, amount }. The amount described is the addition of all allowances signed by the allower. So for example, if **AddrA** signs 2 approvals (allowances) of 50 ELF each, allowing **AddrB** to transfer ELF tokens from **AddrA**, then **AddrB** can transfer (in multiple transactions if needed) a maximum of 50 ELF from **AddrA**s account. 

To transfer tokens from an address other than your own (because someone has approved you), you can use the **TransferFrom** method, it takes the following message as input:

``` Proto
message TransferFromInput {
    Address from = 1;
    // and TransferInput fields
}
```

It's essentially like a normal transfer, but has a **from** address specified. This method will use the allowances that are attributed to the sender by the **from**.
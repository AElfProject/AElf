## Smart contract events

#### Event option

During the execution, Events are used internally to represent events that have happened during the execution of a smart contract. The event will be logged in the transaction traces logs (a collection of LogEvents).

```protobuf
message Transferred {
    option (aelf.is_event) = true;
    aelf.Address from = 1 [(aelf.is_indexed) = true];
    aelf.Address to = 2 [(aelf.is_indexed) = true];
    string symbol = 3 [(aelf.is_indexed) = true];
    sint64 amount = 4;
    string memo = 5;
}
```

Notice the ```option (aelf.is_event) = true;``` line which indicates that the **Transferred** message is destined to be an event.

The following code demonstrates how to fire the event in a contract:

```csharp
Context.Fire(new Transferred()
{
    From = from,
    To = to,
    ...
});
```

External code to the contract can monitor this after the execution of the transaction.
## Smart contract messages

Here we define the concept of the message as defined by the protobuf language. We heavily use these messages to call smart contracts and serializing their state. The following is the definition of a simple message:

```protobuf
message CreateInput {
    string symbol = 1;
    sint64 totalSupply = 2;
    sint32 decimals = 3;
}
```

Here we see a message with three fields of type string, sint64 and sint32. In the message, you can use any type supported by protobuf, including composite messages, where one of your messages contains another message. 

For message and service definitions, we use the **proto3** version of the protobuf language. You probably won't need to use most of the features that are provided, but here's the [full reference](https://developers.google.com/protocol-buffers/docs/proto3) for the language.
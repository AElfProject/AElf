# Serialization in AElf

We use Protobuf to serialize our data structures. The file and code organization is somewhat complex and may need some clarification for some.

Protobuf \(more precisely the protobuf compiler - protoc\) generates CSharp classes. These classes are generated according to descriptions written in protocol buffers own language - Proto3. We keep the definitions and generated classes in a Protobuf folder located in the kernel.

We don't directly use the protobuf generated classes since the kernel already has some data structures defined such as **Transaction** or **Block**. In order to still use our types and the generated classes, we take advantage of the fact that protobuf generates partial classes. This way protobuf generates the properties and we can still write our own methods on the type. It also makes it possible to make the data structure types implement interfaces without having to modify the generated classes.


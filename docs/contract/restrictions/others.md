# Other Restrictions
## GetHashCode Usage
- `GetHashCode` method is only allowed to be called within `GetHashCode` methods. Calling `GetHashCode` methods from other methods is not allowed. This allows developers to implement their custom GetHashCode methods for their self defined types if required, and also allows protobuf generated message types.
- It is not allowed to set any field within `GetHashCode` methods.

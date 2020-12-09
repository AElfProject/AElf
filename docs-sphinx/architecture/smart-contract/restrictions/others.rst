Other Restrictions
==================

GetHashCode Usage
-----------------

- `GetHashCode` method is only allowed to be called within `GetHashCode` methods. Calling `GetHashCode` methods from other methods is not allowed. This allows developers to implement their custom GetHashCode methods for their self defined types if required, and also allows protobuf generated message types.
- It is not allowed to set any field within `GetHashCode` methods.

Execution observer
------------------

- AElf's contract patcher will patch method call count observer for your contract. This is used to prevent infinitely method call like recursion. The number of method called in your contract will be counted during transaction execution. The observer will pause transaction execution if the number exceeds 15,000.

- AElf's contract patcher will patch method branch count observer for your contract. This is used to prevent infinitely loop case. The number of code control transfer in your contract will be counted during transaction execution. The observer will pause transaction execution if the number exceeds 15,000. The control transfer opcodes in C# contract are shown as below.

+---------------------------------+
| Opcode                          |
+=================================+
| ``OpCodes.Beq``                 |
+---------------------------------+
| ``OpCodes.Beq_S``               |
+---------------------------------+
| ``OpCodes.Bge``                 |
+---------------------------------+
| ``OpCodes.Bge_S``               |
+---------------------------------+
| ``OpCodes.Bge_Un``              |
+---------------------------------+
| ``OpCodes.Bge_Un_S``            |
+---------------------------------+
| ``OpCodes.Bgt``                 |
+---------------------------------+
| ``OpCodes.Bgt_S``               |
+---------------------------------+
| ``OpCodes.Ble``                 |
+---------------------------------+
| ``OpCodes.Ble_S``               |
+---------------------------------+
| ``OpCodes.Ble_Un``              |
+---------------------------------+
| ``OpCodes.Blt``                 |
+---------------------------------+
| ``OpCodes.Bne_Un``              |
+---------------------------------+
| ``OpCodes.Bne_Un_S``            |
+---------------------------------+
| ``OpCodes.Br``                  |
+---------------------------------+
| ``OpCodes.Brfalse``             |
+---------------------------------+
| ``OpCodes.Brfalse_S``           |
+---------------------------------+
| ``OpCodes.Brtrue``              |
+---------------------------------+
| ``OpCodes.Brtrue``              |
+---------------------------------+
| ``OpCodes.Brtrue_S``            |
+---------------------------------+
| ``OpCodes.Br_S``                |
+---------------------------------+


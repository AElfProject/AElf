## setup cross chain

Use a main chain and side chain.
Use a main chain and 2 different side chains.

### Node type configuration:

When a node wants to be a side chain for some other blockchain, it will need to configure itself accordingly. Based on the value of the **ChainType** configuration value, the launcher executable will register and launch either a mainchain node or a side chain node. Note that both have many similarities because they’re both based on a common module. Both node types will have a blockchain node context, this provides access to the p2p server and node’s context. When starting an initial list of contracts is loaded. Both chain of course need to be set up with their own chain id.
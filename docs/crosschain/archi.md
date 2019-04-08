## cross chain architecture

## High level architecture

Conceptually a side chain node and mainchain node are similar, they are both independent blockchains, with their own peer-to-peer network and possibly their own ecosystem. It is even possible to have this setup on multiple levels. In terms of peer-to-peer networks, all side chains work in parallel to each other but they are linked to a mainchain node through a cross-chain communication mechanism.

Through this link, messages are exchanged and indexing is performed to ensure that transactions from the main are verifiable in the sidechain. Implementers can use AElf libraries and frameworks to build chains.

One important aspect is the key role that the mainchain plays, because its main purpose is to index the side chains. Side chains are independent and do not have knowledge about each other. This means that when they need to verify information, they need the mainchain to provide the information. Only the mainchain indexes data about all the sidechains.

### Node level architecture

## Client and server:

In the current architecture, both the side chain node and the main chain node has one server and exactly one client. This is the base for AElfs two-way communication between mainchain and side chains. Both the server and the client are implemented as a node plugins (a node has a collection of plugins). Interaction (listening and requesting) can start when both the nodes have started.

### Core:

The project named **AElf.Crosschain.Core** contains the logic outside of the implementation. Even though the side chain module acts as a separate and independent module, it still needs interaction with some of the nodes services. It defines the interactions between the side chain module and the rest of the node. 

**Cache**   
Data issued from the side chains is accessible through a cache. The cache is implemented through two components through the producer/consumer pattern. The cache stores all block info received from the side chain.

**Extra data**  
Get extra data before generating a block.

**Block validation** 
Provides the functionality to validate a block after its execution by the main chain.

**LIB Service** 
It defines an LibService that use the blockchain service to query the last irreversible block of the nodes chain. 

**System transaction generation**  
It also implements a ISystemTransactionGenerator that is used when generating a block (these generators produce transactions that are used to modify the state of the current chain, based on some value specific to the module - side chain module in this case). The generator will use the cross chain service to generate the transactions.


### Contracts:

Token contract (TODO).
Cross chain contract.
	Requests
	Side chain creation
recharge

### Communication protocol:

Step by step description.  
Startup - what happens when the node starts  
Communication - the exchange of messages  

Creation of a side chain process:  
Approve some tokens to the crosschain contract address.  
Request side chain creation through the cross chain contract.  

You can now start the side chain node.  
[Creation of the connection]  
The node starts and itself will start the Plugin  
It sends a handshake (chainId+listening port)  
----- net -----  




IGrpcCrossChainClient ← CrossChainGrpcClient<TResponse> ← GrpcClientForSideChain/GrpcClientForParentChain

### The exposed services:
stream of **ResponseSideChainBlockData** 

A stream that is requested when indexing is requested to start (req: ChainId of requestor + NextHeight). The best chain found will trigger this request. Add the block info to the CrossChainDataProducer.

stream of **RequestIndexingSideChain**
stream of **RequestParentChainDuplexStreaming**
stream of **RequestIndexingParentChain**
CrossChainIndexingShake

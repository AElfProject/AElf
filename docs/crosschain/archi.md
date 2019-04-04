## cross chain architecture

### Node type configuration:

When a node wants to be a side chain for some other blockchain, it will need to configure itself accordingly. Based on the value of the **ChainType** configuration value, the launcher executable will register and launch either a mainchain node or a side chain node. Note that both have many similarities because they’re both based on a common module. Both node types will have a blockchain node context, this provides access to the p2p server and node’s context. When starting an initial list of contracts is loaded. Both chain of course need to be set up with their own chain id.

### Core:

The project named **AElf.Crosschain.Core** contains the logic outside of the implementation. Even though the side chain module acts as a separate and independent module, it still needs interaction with some of the nodes services. It defines the interactions between the side chain module and the rest of the node. 

**Cache**   
Data issued from the side chains is accessible through a cache. The cache is implemented through two components: CrossChainDataConsumer and CrossChainDataProducer. The cache stores all block info received from the side chain.

**Extra data**  
Get extra data before generating a block.

**Block validation** 
Provides the functionality to validate a block after its execution by the main chain.

**LIB Service** 
It defines an LibService that use the blockchain service to query the last irreversible block of the nodes chain. 

**System transaction generation**  
It also implements a ISystemTransactionGenerator that is used when generating a block (these generators produce transactions that are used to modify the state of the current chain, based on some value specific to the module - side chain module in this case). The generator will use the cross chain service to generate the transactions.

### Client and server:

The **Aelf.CrossChain.Grpc** contains the implementation for both client and server that will be used for interchain communication. It is currently implemented with gRPC, but other libraries can be used.

In the current architecture, both the side chain node and the main chain node has one server and exactly one client. This is the base for AElf 2-way communication between chains. 
Both the server and the client are implemented as a node plugins (a node has a collection of plugins, that are contained in the blockchains node context). The IPlugin interface exposes start and stop methods that are called when the node is started or stopped. This means that interaction (listening and requesting) can start when both the node starts.

The service exposed by the server is defined in header_info.proto and grpc is used to generate the C# client/server base class. These methods should be overridden to implement the logic.


### Contracts:

Token contract (TODO).
Cross chain contract.
	Requests
	Side chain creation
recharge


### Events:

Launched by cross chain: GrpcServeNewChainReceivedEvent
Handled by other modules: --
Handled by cross chain module: BestChainFoundEventData, GrpcServeNewChainReceivedEvent
BestChainFoundEventData: request indexing.
GrpcServeNewChainReceivedEvent: controller create client.

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

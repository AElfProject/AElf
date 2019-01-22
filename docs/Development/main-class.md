```puml


package AElf.Kernel.SmartContracts.Consensus{
    class ConsensusSmartContract
}

package AElf.Kernel.SmartContracts.Consensus.Abstractions{
    interface IConsensusSmartContract
    IConsensusSmartContract <|-- ConsensusSmartContract
}
note top of AElf.Kernel.SmartContracts.Consensus.Abstractions: infrastructure layer

package AElf.Kernel{
    interface IBlockchainService{

    }

    interface ISmartContractExecutingService{

    }

    IBlockchainService --> ISmartContractExecutingService
    IBlockchainService --> ISmartContractRuntimeFactory
    IBlockchainService --> ISmartContractRuntime
    IBlockchainService --> IBlockService

    interface ISmartContractRuntimeFactory{
        ISmartContract CreateRuntime(int category)
    }

    interface ISmartContractRuntime

    ISmartContractRuntimeFactory --> ISmartContractRuntime

    interface IBlockService

    class BlockService
    IBlockService <|-- BlockService

    interface IBlockManager
    class BlockManager
    IBlockManager <|-- BlockManager

    BlockService -> IBlockManager

    BlockManager --> IConsensusSmartContract : use IConsensusSmartContract to validate BlockHeader, maybe in BlockHeaderManager

}

package AElf.Kernel.Runtimes.CSharp{
    ISmartContractRuntime <|-- CSharpSmartContractRuntime
}

package AElf.Kernel.Runtimes.NodeJS{
    ISmartContractRuntime <|-- NodeJSSmartContractRuntime

}

package AElf.Kernel.Akka{
    class AkkaSmartContractExecutingService{

    }

    AkkaSmartContractExecutingService <|-- ISmartContractExecutingService

}



package AElf.OS{
    interface INetworkService{

    }

    class NetworkService{

    }

    INetworkService <|-- NetworkService
    NetworkService --> IPeerManager
    NetworkService --> INetworkManager

    interface INetworkManager{

    }

    interface IPeerManager{

    }

    class NewBlockAnnouncementEventHandler
    NewBlockAnnouncementEventHandler -> INetworkService
    NewBlockAnnouncementEventHandler -> IBlockchainService

}

package AElf.OS.Networks.Grpc{
    class GrpcNetworkManager{

    }

    INetworkManager <|-- GrpcNetworkManager

    class GrpcServer{

    }

    GrpcNetworkManager --> GrpcServer
}



```

AElf.Kernel and AElf.OS are two DDD module.
AElf.OS references AElf.Kernel
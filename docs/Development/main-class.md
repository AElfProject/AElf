```puml


package AElf.Kernel.SmartContracts.Consensus{
    class ConsensusSmartContract

    ConsensusBlockHeaderDataProvider --> IConsensusSmartContract : GetBlockHeaderConsensusData
}

package AElf.Kernel.SmartContracts.Consensus.Abstractions{
    interface IConsensusSmartContract{
        byte[] GetBlockHeaderConsensusData()
    }
    IConsensusSmartContract <|-- ConsensusSmartContract
}

note top of AElf.Kernel.SmartContracts.Consensus.Abstractions: infrastructure layer

package AElf.Kernel.SmartContracts.Genesis{
    class GenesisSmartContract

    interface IBlockHeaderDataProvider
    IBlockHeaderDataProvider <|-- ConsensusBlockHeaderDataProvider
    
    class BlockHeaderExtraDataProviderContainer{
        List<IBlockHeaderDataProvider> ExtraBlockHeaderDataProviders

        AddDataProvider<T>(int Order)

        void Fill(BlockHeader blockHeader)
        bool Validate(BlockHeader blockHeader)
    }

    note top of BlockHeaderExtraDataProviderContainer: singleton, and AddDataProvider in the module to build a blockchain

}

package AElf.Kernel.SmartContracts.Genesis.Abstractions{
    interface IGenesisSmartContract{
    }
    IGenesisSmartContract <|-- GenesisSmartContract
}

package AElf.Kernel.SmartContracts.Genesis.MainChain{
    class MainChainGenesisSmartContract
    GenesisSmartContract <|-- MainChainGenesisSmartContract

}

package AElf.Kernel.SmartContracts.Genesis.SideChain{
    class SideChainGenesisSmartContract
    GenesisSmartContract <|-- SideChainGenesisSmartContract
    IBlockHeaderDataProvider <|-- SideChainBlockHeaderDataProvider

}

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

    ConsensusService --> IConsensusSmartContract

    interface IBlockHeaderManager
    class BlockHeaderManager
    IBlockHeaderManager <|-- BlockHeaderManager

    BlockService --> IBlockHeaderManager
    interface IContractReader

    interface IMinerService{
        Block Mine()
    }
    
    interface ICrossChainTransactionGenerator
    interface ICrossChainTransactionValidator

    IMinerService --> IBlockchainService
    IMinerService --> ICrossChainTransactionGenerator
    
    interface IConsensusService {
        IDisposable ConsensusObservables
        IBlockHeaderDataProvider GenerateConsensusBlockHeaderDataProvider()
    }

    class ConsensusService

    ConsensusService <|-- IConsensusService
    
    interface IConsensusManager {
        bool ValidateConsensus(byte[] consensusInformation)
        bool GetNextMiningTime(out ulong distance)
        byte[] GenerateNewConsensus()
    }

    ConsensusService --> IConsensusManager : Use GetNextMiningTime() to update ConsensusObservables\nUse GenerateNewConsensus() to get new consensus information

    ConsensusService --> IMinerService

    interface IAccountService{
        Task<byte[]> Sign(byte[] data)
        Task<bool> VerifySignature(byte[] signatureBytes, byte[] data)
        Task<byte[]> GetPublicKey()
    }
}


package AElf.Kernel.Types{
    class BlockHeader{
        byte[] ExtraBlockHeaderData
    }
}

package AElf.Kernel.Runtimes.CSharp{
    ISmartContractRuntime <|-- CSharpSmartContractRuntime
    class ContractReader
    IContractReader <|-- ContractReader
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

    class AccountService{
        
    }
    IAccountService <|-- AccountService

}

package AElf.OS.Networks.Grpc{
    class GrpcNetworkManager{

    }

    INetworkManager <|-- GrpcNetworkManager

    class GrpcServer{

    }

    GrpcNetworkManager --> GrpcServer
}

package AElf.CrossChain.Grpc{

    interface ICrossChainService{
        ParentChainBlockInfo[] TryGetParentChainBlockInfo(ParentChainBlockInfo[])
        bool TryGetSideChainBlockInfo(SIdeChainBlockInfo[])
        SideChainBlockInfo[] CollectSideChainBlockInfo()
    }

    class CrossChainService 
    ICrossChainService <|-- CrossChainService
    class ClientBase<T> {
      T[] ToBeIndexedInfoQueue
      T TryTake()
      void StartServerStreamingCall()
    }
    class ClientToParentChain{
      void Call()
    }

    class ClientToSideChain{
      void Call()
    }
    ClientBase <|-- ClientToParentChain
    ClientBase <|-- ClientToSideChain

    CrossChainService --> ClientToSideChain
    CrossChainService --> ClientToParentChain

    class ParentChainBlockInfoServer
    class SideChainBlockInfoServer
    class CrossChainTransactionGenerator
    ICrossChainTransactionGenerator <|-- CrossChainTransactionGenerator
    CrossChainTransactionGenerator --> ICrossChainService
    CrossChainTransactionValidator --> ICrossChainService

    class CrossChainTransactionValidator 
    ICrossChainTransactionValidator <|-- CrossChainTransactionValidator
    
    ParentChainBlockInfoServer --> IContractReader

}

package AElf.Consensus.DPoS {
    class DPoSManager
    class DPoSInfoServer
    DPoSInfoServer --> IContractReader
    IConsensusManager <|--  DPoSManager
}

package AElf.Consensus.PoW {
    class PoWManager
    class PoWInfoServer
    PoWManager --> IContractReader
    IConsensusManager <|-- PoWManager
}



```

AElf.Kernel and AElf.OS are two DDD module.
AElf.OS references AElf.Kernel

When a component in lower layer, such as infrastructure need to work with manager to get some data, we need to implement a service.

for example, maybe IConsensusSmartContract need to work with state manager or smart contract execution manager. so we may have to make a service and make them work together.

and also, we cannot call a service in manager, we need to call it in another service. service can have dependencies of other services.

Hello

```puml
@startuml
scale max 768 width

package "Application" {
  [Service]
  [Saga]
  
  [FSM]
  
  [Saga] -> [Service] : injection

  [EventHandler] -> [Saga] : injection
  [EventHandler] -> [Service] : injection

}

package "Domain" {
  [Manager]
  [Context]
  [Event]
  [Entity]
  [FacadeService] -> [Manager]

  
  [EventHandler] <.. [Event] : subscribe


  [Saga] ..> [Context] : create
  [Saga] -> [FSM] : injection
  [Service] ..> [Context] : parameters
  [Context] -> [Manager] 


  [Service] -> [Manager]

  [Manager] -> [Entity]

  [Manager] ..> [Event] : publish

note top of [Context]
  context is just use to share data
end note
}


@enduml
```
AELF project

```puml
@startuml

package "AElf.Kernel.Application" {
  [Service]
}

package "AElf.Kernel.Domain" {
  [Entity]
  
}


@enduml
```


Some basic defines
```csharp

//protobuf class
public class ChainId{
  public string ChainPrefix{get;set;}
}

public class LoadChainRequest{
  public Guid SagaId{get;set;}
  public ChainId ChainId{get;set;}
}

```


One way is to define a initialize method in IBlockChain, the problem is in some scenes, the initialize method may be call more than one time.but it is okay, because chainID is not status, ChainInfo will be loaded from DB by ChainInfoManager. 
```csharp

//registered as scoped
interface IBlockChain{
  void Initialize(ChainId Id);
  //other methods...
}

//protobuf entity
public class ChainInfo{
  public ChainID ChainID{get;set;}
  public long CurrentHeight{get;set;}
  public Hash CurrentBlockHash{get;set;}
  //.....
}

public class Blockchain: IBlockChain{

  ChaiId _chainId;

  public BlockChain(ChainInfoManager chainInfoManager){
    //...
  }


  void Initialize(ChainId Id){
    if(_chainID=!null && _chainID!=Id)
      throw new Exception("Invalid");
    _chainID = Id;
  }

  Task DoSomethingAsync(){
    var chainInfo = await _chainInfoManager.GetAsync(_chainId);
    //....
  }
}


public class OneApplicationService{

  private IBlockChain _blockchain;

  public Task OneRequest(RequestDto input){
    _blockchain.initialize(input.ChainId);
    _blockchain.XXX();
    //....
  }
}

//usage in service layer
public class OneSaga{
  
  //can implement as saga data manager. _chainId is a saga data.
  private ChainId _chainId;
  private SagaId _sagaId;

  //inject 
  private ISmartContractDeployService _smartContractDeployService;

  private IBlockChain _blockchain;

  public OneSaga(IBlockChain blockchain /*, ...*/){
    _blockchain = blockchain;
    
    /*...*/
  }

  //maybe in base class
  public OneSaga(SagaId id){
    _sagaId=id;
    OnSagaLoadAsync();
  }

  protected Task LoadSagaDataAsync(){
    _chainId = await GetSagaDataAsync(_sagaId, "_chainId");
    blockchain.Initialize(_chainId);
  }

  protected Task SaveSagaDataAsync(){
    await SetSagaDataAsync(_sagaId,"_chainID",_chainId);
  }

  //Here block chain logic begins

  //in the first saga request to 
  public Task LoadChainAsync(LoadChainRequest request){
    _sagaId = _chainId = request.ChainId;
    blockchain.Initialize(request.ChainId);
  }

  public Task DeploySmartContractAsync(SmartContractDeployRequest request){

    //_smartContractDeployService.XXX()
    //...
  }

}

```


The other way, inject chainContext, chainContext was injected as scoped.
so maybe we need to override scoped factory to make sure when we get the context in nest we can get the same context.

But when we saw IBlockChain, we may not see anything about chainID. It's not a good choice for me.
```csharp
using(var scope = provider.BeginScope()){
    var chainContext1= scope.GetService<ChainContext>();
  using(var scope2 = scop.BeginScope()){
    var chainContext2= scope.GetService<ChainContext>();

    //chainContext1 should equal to chainContext2
  }

}
```

```csharp

//registered as scoped
interface IBlockChain{
  //other methods...
}

//registered as scoped
public class ChainContext{

  //chainID should can only be set once.
  public ChainId ChainId{get;set;}
}

public class Blockchain: IBlockChain{

  ChaiId _chainId;

  //inject chain context
  public BlockChain(ChainContext chainContext, ChainContextManager chainContextManager){
    //...
  }


  void Initialize(ChainId Id){
    if(_chainID=!null && _chainID!=Id)
      throw new Exception("Invalid");
    _chainID = Id;
  }

  Task DoSomethingAsync(){
    var chainInfo = await _chainInfoManager.GetAsync(chainContext.ChainId);
    //....
  }
}


public class OneApplicationService{

  private IBlockChain _blockchain;
  private ChainContext _chainContext;

  public Task OneRequest(RequestDto input){
    
    _chainContext.ChainId = input.ChainId;

    _blockchain.XXX(input.other);
    //....
  }
}

//usage in service layer
public class OneSaga{
  
  //can implement as saga data manager. _chainId is a saga data.
  private ChainContext _chainContext;
  private SagaId _sagaId;

  //inject 
  private ISmartContractDeployService _smartContractDeployService;

  private IBlockChain _blockchain;

  public OneSaga(IBlockChain blockchain /*, ...*/){
    _blockchain = blockchain;
    
    /*...*/
  }

  //maybe in base class
  public OneSaga(SagaId id){
    _sagaId=id;
    LoadSagaDataAsync();
  }

  protected Task LoadSagaDataAsync(){
    _chainContext.ChainId = await GetSagaDataAsync(_sagaId, "_chainId");

  }

  protected Task SaveSagaDataAsync(){
    await SetSagaDataAsync(_sagaId,"_chainID",_chainId);
  }

  //Here block chain logic begins

  //in the first saga request to 
  public Task LoadChainAsync(LoadChainRequest request){
    _sagaId = _chainContext.ChainId = request.ChainId;
  }

  public Task DeploySmartContractAsync(SmartContractDeployRequest request){

    //_smartContractDeployService.XXX()
    //...
  }

}

```

The third way, pass chainId by parameters.
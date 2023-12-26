Build Indexer
=============

Step 1. Subscribe block information
------------------------------------

**Obtaining Authentication Authorization**

The demand side (DApp) needs to contact the indexer system administrator first to get the client ID and key assigned by the indexer, which looks like this:

.. code-block:: json

  {
    "ClientId": "Sample_DApp",
    "ClientSecret": "1q2w3e*"
  }

Each DApp that requires an indexer should apply for a corresponding client ID and key, which will be valid for a long time.

Upon obtaining the client ID and key pre-allocated by the AElf Indexer, you can initiate an authentication authorization request to the Indexer, obtaining an authentication token (Token) upon successful verification.

**Post request address** : http://URL:{Port}/connect/token

The URL and port correspond to the server address where the AElf Indexer AuthServer service is located. Please contact AElf to get it.

**Request Body** (x-www-form-urlencoded):

.. code-block:: text

  grant_type:client_credentials
  scope:AElfIndexer
  client_id:Sample_DApp
  client_secret:1q2w3e*

**Response**:

.. code-block:: json

  {
    "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IkY1RDFFRjAzRDlEMEU2MTI1N0ZFMTc0ODVBRkI2RjUzNDc0QzJEQjkiLCJ4NXQiOiI5ZEh2QTluUTVoSlhfaGRJV3Z0dlUwZE1MYmsiLCJ0eXAiOiJhdCtqd3QifQ.eyJvaV9wcnN0IjoiQUVsZkluZGV4ZXJfREFwcCIsImNsaWVudF9pZCI6IkFFbGZJbmRleGVyX0RBcHAiLCJvaV90a25faWQiOiI5MTljZmYzOC0xNWNhLTJkYWUtMzljYi0zYTA4YzdhZjMxYzkiLCJhdWQiOiJBRWxmSW5kZXhlciIsInNjb3BlIjoiQUVsZkluZGV4ZXIiLCJleHAiOjE2NzM3OTEwOTYsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6ODA4My8iLCJpYXQiOjE2NzM3ODc0OTZ9.aABo_opBCiC3wePnIJpc6y3E4-nj50_WP93cYoYwxRGOxnXIq6LXz_r3-V_rmbzbxL3TbQvWQVuCcslF_rUJTMo6e6WC1ji5Ec9DtPpGbOOOvYALNhgOiP9p9TbzVubxHg7WdT6OEDLFihh4hsxtVBTK5_z8YXTa7fktLqve5Bd2eOpjb1TnQC7yZMwUvhnvQrjxuK9uRNxe9ODDt2EIcRhIQW5dQ-SDXpVoNfypY0GxQpuyHjwoJbtScJaX4HfHbh0Fis8EINOwpJr3-GKtcS6F4-t4FyOWMVW19y1_JAoCKTUlNy__htpdMOMQ-5nmFYYzlNr27LSOC_cylXz4lw",
    "token_type": "Bearer",
    "expires_in": 3593
  }

  
The access_token is the authentication token. It is required when making specific subscription requests to the Subscription API.

**Send Subscription**

By sending a request to the Subscription API, you inform the Indexer system that your DApp needs to subscribe to specific blocks, transactions, or events. Subsequently, when the interface plugin subscribes to the Indexer client, the Indexer system filters out the specified blocks/transactions/events and pushes them to the corresponding interface plugin.

**Post request address** : http://URL:{Port}/api/app/subscription

Request Mode：raw

Request Header: Authorization Bearer {access_token}

**Request Body**:

.. code-block:: json

    [
        {
            "chainId": "tDVV",
            "startBlockNumber": 48532699,
            "onlyConfirmedBlock": false,
            "filterType" : "Transaction",
            "subscribeEvents": [
                {
                    "contractAddress": "{contract_address}", //the address of the contract you deployed
                    "eventNames": [
                        "SampleEvent" //the event defined in the contract you want to index
                    ]
                }
            ]
        }
    ]

Parameters Explanation:

+----------------------+----------------------------------------------------------------+
| Parameter            | Description                                                    |
+======================+================================================================+
| ChainId              | The AElf chain ID to subscribe, e.g., AElf mainchain is AElf,  |
|                      | sidechain is tDVV.                                             |
+----------------------+----------------------------------------------------------------+
| StartBlockNumber     | The initial push block height for subscription, usually this   |
|                      | will be the block height of the contract deployed.             |
+----------------------+----------------------------------------------------------------+
| OnlyConfirmedBlock   | Whether only confirmed blocks are subscribed or not.           |
+----------------------+----------------------------------------------------------------+
| FilterType           | The type of block data to be subscribed. Currently, the indexer|
|                      | system categorizes a complete block data into three levels of  |
|                      | data structures: Block, Transaction, and Logevent. For details,|
|                      | refer to the Scanning Data Structure Example.                  |
+----------------------+----------------------------------------------------------------+
| SubscribeEvents      | The subscribed events.                                         |
+----------------------+----------------------------------------------------------------+


After successfully calling the API, the version of subscription will be returned, e.g. ``932e5a54b6044e049cf939607b248d89``

Note down this version number, as it will be used in the development of the client interface plugin in Step 2.

**Get Existing Subscription**

If you need to view all the initiated subscription information, you can query it through the following API.

**Get request address** : http://URL:{Port}/api/app/subscription

Request Header: Authorization Bearer {access_token}

**Response**:

.. code-block:: json

    {
        "currentVersion": {
            "version": "932e5a54b6044e049cf939607b248d89",
            "subscriptionInfos": [
                {
                    "chainId": "tDVV",
                    "startBlockNumber": 48532699,
                    "onlyConfirmedBlock": false,
                    "filterType": 1,
                    "subscribeEvents": [
                        {
                            "contractAddress": "{contract_address}",
                            "eventNames": [
                                "SampleEvent"
                            ]
                        }
                    ]
                }
            ]
        },
        "newVersion": null
    }

**Stop Running Subscription**

**Post request address** : http://URL:{port}/api/app/block-scan/stop?version={subscription_version}
 
This API is used to stop running subscriptions.

Request Header: Authorization Bearer {access_token}
               
**Replace Running Subscription by New Subscription**

**Post request address** :  http://URL:{port}/api/app/block-scan/upgrade
 
This API is used to replace current subscription version by new version. After a new subscription is created, 
it will be at "newVersion". When it's ready to use, this API is required to be called to upgrade it to currentVersion.

.. image:: subscription_version.jpeg
   :alt: Subscription Version    

Request Header: Authorization Bearer {access_token}     

**Update Running Subscription**

**Post request address** : http://URL:{Port}/api/app/subscription/{Version}

Request Mode：raw

Request Header: Authorization Bearer {access_token}

**Request Body**:

.. code-block:: json

    [
        {
            "chainId": "AELF",
            "startBlockNumber": 54541,
            "onlyConfirmedBlock": false,
            "filterType": "LogEvent",
            "subscribeEvents": [
                {
                    // update content
                }
            ]
        }
    ]
    
Step 2. Indexer Plugin Development
------------------------------------

Having understood the working principle of the AElf Indexer, you will find that to 
enable a DApp to request data from the AElf Indexer, the main task is to develop a 
client interface plugin.

.. image:: indexer-plugin.png
   :alt: Indexer Plugin

The following will use a sample as an example to explain in detail how to develop a client interface plugin.

A sample indexer project repo: ``https://github.com/xibo1/aelf-indexer-demo/tree/dev``

A completed indexer project repo: ``https://github.com/Portkey-Wallet/bingo-game-indexer``

**Development Environment**

.Net 7.0

**Building the Project Skeleton**

1. Build a .Net 7.0 empty project 

2. Create 2 main folders, one is src , another is test

The src folder will contain the code of the indexer plugin, the test folder will contain the code of unit test for the indexer plugin.

3. Add the required package

Under src folder, create project file <Your_own_indexer>.Indexer.csproj. Import AElfIndexer.Client package. The latest version of this package is "1.0.0-28"

Here is the sample code of it:

.. code:: xml

    <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
            <TargetFramework>net7.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
        </PropertyGroup>
        <ItemGroup>
          <PackageReference Include="AElfIndexer.Client" Version="1.0.0-28" />
        </ItemGroup>
    </Project>
    
4.  Build the src skeleton

Under src folder, create these subfolders: Contract, Enities, GraphQL, Handler and Processors
These folders will contain different parts of the indexer plugin.

Contract: This folder will contain the generated files of your contract, which end with c.cs and g.cs, e.g. HelloWorldContract.c.cs and HelloWorldContract.g.cs. These will be used for telling indexer plugin the event datastructure defined in the contract.

Entities: This folder will contain the files defining datastructure which will be used for storing and querying data.

GraphQL: This folder will contain the files defining the interface of querying data from storage and the datastructure which will be used for the GraphQL interface.

Handler: This folder will contain handlers about how to handle block data.

Processors: This folder will contain processors. These processors are the specific logic about how to process the indexed data and store data into storage.

5. Add contract files to the project

Move the generated contract files to ``src/Contractfolder``. These generated contract files end with c.cs and g.cs. They can be found under the path ``<AElf_Contract_Project>/Protobuf/Generated``

6. Define datastructures
 
After the interface plugin receives the corresponding block information data from the AElf Indexer Client, it needs to process the block data for each height according to the custom code logic. The processed results should be updated and stored in the index library. In general, behind each interface, there is a corresponding index library that stores its result set.

Currently, the AElf Indexer system supports using ElasticSearch as the medium for persistent storage of index libraries. However, the entity class for the index library structure of the result set needs to be defined manually, inheriting from AElfIndexerClientEntity and implementing the IIndexBuild interface.

This entry refers to the data structure utilized when storing information into ElasticSearch after processing the data obtained through AElf Indexer.

Create a file IndexEntry.cs under src/Entities folder. Here is the sample code of it:

.. code:: c#

    using AElf.Indexing.Elasticsearch;
    using AElfIndexer.Client;
    using Nest;
    
    namespace Sample.Indexer.Entities
    {
        public class SampleIndexEntry : AElfIndexerClientEntity<string>, IIndexBuild
        {
            // Define it according to your own usage requirements.
            [Keyword]
            public string FromAddress { get; set; }
            
            public long Timestamp { get; set; }
            
            public long Amount { get; set; }
           
            // Define it according to your own usage requirements.
        }
    }

7. Creating the GraphQL query interface

This interface will serve as the user's interface for querying data. It should include the logic based on which GraphQL returns data to the user when querying. This will be talked about in GraphQL interface development section.

Create a file Query.csunder src/GraphQL. Here is the sample code of it:

.. code:: c#

    using AElfIndexer.Client;
    using AElfIndexer.Grains.State.Client;
    using GraphQL;
    using Nest;
    using Sample.Indexer.Entities;
    using Volo.Abp.ObjectMapping;
    
    namespace Sample.Indexer.GraphQL
    {
        public class Query
        {
            public static async Task<SampleResultDto> SampleIndexerQuery(
                [FromServices] IAElfIndexerClientEntityRepository<SampleIndexEntry, LogEventInfo> repository,
                [FromServices] IObjectMapper objectMapper,  QueryDto dto)
            {
                // Define it according to your own usage requirements.
                var infoQuery = new List<Func<QueryContainerDescriptor<SampleIndexEntry>, QueryContainer>>();
                if (dto.PlayerAddress == null)
                {
                    return new SampleResultDto();
                }
                infoQuery.Add(q => q.Terms(i => i.Field(f => f.FromAddress).Terms(dto.PlayerAddress)));
                var result = await repository.GetSortListAsync(
                    f => f.Bool(b => b.Must(infoQuery)), 
                    sortFunc: s => s.Descending(a => a.Timestamp));
                var dataList = objectMapper.Map<List<SampleIndexEntry>, List<TransactionData>>(result.Item2);
                var queryResult = new SampleResultDto
                {
                    Data = dataList
                };
                return queryResult;
                // Define it according to your own usage requirements.
            }
        }

8. Create the GraphQL structure class 

Create a file IndexerSchema.cs under src/GraphQL. Here is the sample code of it:

.. code:: c#

    using AElfIndexer.Client.GraphQL;
    
    namespace Sample.Indexer.GraphQL
    {
        public class IndexerSchema : AElfIndexerClientSchema<Query>
        {
            public IndexerSchema(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }
    }

9. Define datastructure for Query 

Besides the schema and query logic, datastructures used in Query also need to be defined. At least 2 datastructures 
are needed. One is QueryDto, which is the input for querying data, another one is ResultDto, which is the output.
Create a file Dto.csunder src/GraphQL. Here is the sample code of it:

.. code:: c#

    using GraphQL;
    using Volo.Abp.Application.Dtos;
    
    namespace Sample.Indexer.GraphQL
    {
        public abstract class QueryDto: PagedResultRequestDto
        {
            
            [Name("playerAddress")]
            public string PlayerAddress { get; set; }
        }
        
        public class ResultDto
        {
            public List<TransactionData> Data { get; set; }
        }
        
        public class TransactionData
        {
            public string FromAddress { get; set; }
            
            public long Timestamp { get; set; }
            
            public long Amount { get; set; }
        }
    }

10. Build processors

Depending on the subscribed block information type (Block/Transaction/LogEvent), the processing methods 
for each may vary slightly.

Transaction

Processing transaction structure type block transaction data mainly involves handling TransactionInfo. 
To do this, you need to inherit from the ``AElfLogEventProcessorBase`` class, and override and implement its 
``GetContractAddress`` and ``HandleEventAsync`` methods.

.. code:: c#

    public abstract class SampleTransactionProcessor : AElfLogEventProcessorBase<SampleEvent, TransactionInfo>
    {
        protected readonly IAElfIndexerClientEntityRepository<SampleTransactionIndex, TransactionInfo> SampleTransactionIndexRepository;
        protected readonly IAElfIndexerClientEntityRepository<SampleIndex, LogEventInfo> SampleIndexRepository;
        protected readonly ContractInfoOptions ContractInfoOptions;
        protected readonly IObjectMapper ObjectMapper;
    
        protected SampleTransactionProcessor(ILogger<SampleTransactionProcessor> logger,
            IAElfIndexerClientEntityRepository<SampleIndex, LogEventInfo> sampleIndexRepository,
            IAElfIndexerClientEntityRepository<SampleTransactionIndex, TransactionInfo> sampleTransactionIndexRepository,
            IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
            IObjectMapper objectMapper) : base(logger)
        {
            SampleTransactionIndexRepository = sampleTransactionIndexRepository;
            SampleIndexRepository = sampleIndexRepository;
            ContractInfoOptions = contractInfoOptions.Value;
            ObjectMapper = objectMapper;
        }
    
        public override string GetContractAddress(string chainId)
        {
            return ContractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).SampleContractAddress;
        }
    
        protected override async Task HandleEventAsync(SampleEvent eventValue, LogEventContext context)
        {
            // implement your handling logic here
        }
    }

LogEvent

Processing block transaction data of LogEvent structure type primarily involves handling LogEventInfo. 
To do this, you need to inherit from the ``AElfLogEventProcessorBase`` class, override and implement its 
``GetContractAddress`` and ``HandleEventAsync`` methods.

.. code:: c#

    public class SampleLogEventProcessor : AElfLogEventProcessorBase<SampleEvent, LogEventInfo>
    {
        private readonly IAElfIndexerClientEntityRepository<SampleIndex, LogEventInfo> _repository;
        private readonly ContractInfoOptions _contractInfoOptions;
        private readonly IObjectMapper _objectMapper;
    
        public NFTProtocolCreatedProcessor(ILogger<SampleLogEventProcessor> logger, IObjectMapper objectMapper,
            IAElfIndexerClientEntityRepository<SampleIndex, LogEventInfo> repository,
            IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
        {
            _objectMapper = objectMapper;
            _repository = repository;
            _contractInfoOptions = contractInfoOptions.Value;
        }
    
        public override string GetContractAddress(string chainId)
        {
            return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).SampleContractAddress;
        }
    
        protected override async Task HandleEventAsync(SampleEvent eventValue, LogEventContext context)
        {
            // implement your handling logic here
        }
    }

Block

Processing block structure type block data mainly involves handling BlockInfo. To do this, you need to inherit 
from the ``BlockDataHandler`` class and override and implement its ``ProcessDataAsync`` method.

.. code:: c#

    public class SampleBlockProcessor : BlockDataHandler
    {
        private readonly IAElfIndexerClientEntityRepository<SampleIndex, BlockInfo> _repository;
    
        public SampleBlockProcessor(IClusterClient clusterClient, IObjectMapper objectMapper,
            IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider,
            IAElfIndexerClientEntityRepository<SampleIndex, BlockInfo> repository,
            ILogger<SampleBlockProcessor> logger) : base(clusterClient, objectMapper, aelfIndexerClientInfoProvider, logger)
        {
            _repository = repository;
        }
    
        protected override async Task ProcessDataAsync(List<BlockInfo> data)
        {
            foreach (var block in data)
            {
                var index = ObjectMapper.Map<BlockInfo, SampleIndex>(block);
                Logger.LogDebug(index.ToJsonString());
                await _repository.AddOrUpdateAsync(index);
            }
        }
    
        protected override Task ProcessBlocksAsync(List<BlockInfo> data)
        {
            // implement your handling logic here
        }
    }

Create ``<Your_processor>.cs`` under ``src/Processors`` folder. Here is the sample code:

.. code:: c#

    using AElfIndexer.Client;
    using AElfIndexer.Client.Handlers;
    using AElfIndexer.Grains.State.Client;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Sample.Indexer.Entities;
    using AElf.Contracts.HelloWorld;
    using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;
    
    namespace Sample.Indexer.Processors
    {
        public class SampleTransactionProcessor : AElfLogEventProcessorBase<SampleEvent, TransactionInfo>
        {
            private readonly IAElfIndexerClientEntityRepository<SampleIndexEntry, TransactionInfo> _sampleIndexRepository;
            private readonly ContractInfoOptions _contractInfoOptions;
            private readonly IObjectMapper _objectMapper;
        
            public SampleTransactionProcessor(ILogger<SampleTransactionProcessor> logger,
                IAElfIndexerClientEntityRepository<SampleIndexEntry, TransactionInfo> sampleIndexRepository,
                IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
                IObjectMapper objectMapper) : base(logger)
            {
                _sampleIndexRepository = sampleIndexRepository;
                _objectMapper = objectMapper;
                _contractInfoOptions = contractInfoOptions.Value;
            }
        
            public override string GetContractAddress(string chainId)
            {
                return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).SampleContractAddress;
            }
        
            protected override async Task HandleEventAsync(SampleEvent eventValue, LogEventContext context)
            {
                if (eventValue.PlayerAddress == null)
                {
                    return;
                }
                
                var indexEntry = new SampleIndexEntry
                {
                    Id = eventValue.PlayerAddress,
                    FromAddress = eventValue.PlayerAddress,
                    Timestamp = eventValue.Timestamp,
                    Amount = eventValue.Amount
                };
                _objectMapper.Map(context, indexEntry);
                await _sampleIndexRepository.AddOrUpdateAsync(indexEntry);
            }
        }
    }

11. Register Processors and other indexer plugin services
This module inherits from the AElfIndexer plugin base class. It configures and registers services.
Create the project file ``IndexerModule.cs`` under ``src`` folder. Here is the sample code of it:

.. code:: c#

    using AElfIndexer.Client;
    using AElfIndexer.Client.Handlers;
    using AElfIndexer.Grains.State.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Sample.Indexer.GraphQL;
    using Sample.Indexer.Handlers;
    using Sample.Indexer.Processors;
    using Volo.Abp.Modularity;
    
    namespace Sample.Indexer
    {
        [DependsOn(typeof(AElfIndexerClientModule))]
        public class SampleIndexerModule : AElfIndexerClientPluginBaseModule<SampleIndexerModule, IndexerSchema, Query>
        {
            protected override void ConfigureServices(IServiceCollection serviceCollection)
            {
                var configuration = serviceCollection.GetConfiguration();
                serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, SampleTransactionProcessor>();
                serviceCollection.AddTransient<IBlockChainDataHandler, SampleHandler>();
                // register your own processors and service here
                Configure<ContractInfoOptions>(configuration.GetSection("ContractInfo"));
            }
        
            protected override string ClientId => "";
            protected override string Version => "";
        }
    }

    
Step 3. Testing
------------------------------------

Testing the Indexer plugin locally can be complex as it requires simulating the entire Indexer application. It is 
recommended to directly pull the "test" directory from this repository 
``https://github.com/xibo1/aelf-indexer-demo/tree/dev`` for a more straightforward testing environment.

Then, add the necessary test cases in the ``Sample.Indexers.Tests`` folder. The basic idea of writing test cases is 
to simulate the input data of processors, then check if the data can be queried from elasticsearch.
Here is the sample code of a unit case:

.. code:: c#

    [Fact]
    public async Task HandleSampleEvent_Test()
    {
        const string chainId = "AELF";
        const string blockHash = "3c7c267341e9f097b0886c8a1661bef73d6bb4c30464ad73be714fdf22b09bdd";
        const string previousBlockHash = "9a6ef475e4c4b6f15c37559033bcfdbed34ca666c67b2ae6be22751a3ae171de";
        const string transactionId = "c09b8c142dd5e07acbc1028e5f59adca5b5be93a0680eb3609b773044a852c43";
        const long blockHeight = 200;
        var blockStateSetAdded = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash
        };
        
        var blockStateSetTransaction = new BlockStateSet<TransactionInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSetAdded, chainId);
        var blockStateSetKeyTransaction = await InitializeBlockStateSetAsync(blockStateSetTransaction, chainId);
        var sampleEvent = new SampleEvent
        {
            PlayerAddress = Address.FromPublicKey("AAA".HexToByteArray()).ToString()?.Trim('\"'),
            Timestamp = 1702968980,
            Amount = 100000000
        };
        var logEventInfo = new LogEventInfo
        {
            ExtraProperties = new Dictionary<string, string>
            {
                { "Indexed", sampleEvent.ToLogEvent().Indexed.ToString() ?? string.Empty },
                { "NonIndexed", sampleEvent.ToLogEvent().NonIndexed.ToBase64() }
            },
            BlockHeight = blockHeight,
            ChainId = chainId,
            BlockHash = blockHash,
            TransactionId = transactionId
        };
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId,
            Params = "{ \"to\": \"ca\", \"symbol\": \"ELF\", \"amount\": \"100000000000\" }",
            To = "CAAddress",
            MethodName = "Played",
            ExtraProperties = new Dictionary<string, string>
            {
                { "TransactionFee", "{\"ELF\":\"30000000\"}" },
                { "ResourceFee", "{\"ELF\":\"30000000\"}" }
            },
            BlockTime = DateTime.UtcNow
        };
        var sampleProcessor = GetRequiredService<SampleTransactionProcessor>();
        await sampleProcessor.HandleEventAsync(logEventInfo, logEventContext);
        sampleProcessor.GetContractAddress(chainId);
    
        // step 4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await BlockStateSetSaveDataAsync<TransactionInfo>(blockStateSetKeyTransaction);
        await Task.Delay(2000);
        
        var sampleIndexData = await _sampleIndexRepository.GetAsync(Address.FromPublicKey("AAA".HexToByteArray()).ToString()?.Trim('\"'));
        sampleIndexData.ShouldNotBeNull();
        sampleIndexData.Amount.ShouldBe(100000000);
    }

Before running the test cases, elasticsearch is also needed. The latest version of it can be downloaded from: 
``https://www.elastic.co/downloads/elasticsearch`` 

Step 4. Deployment of Indexer
------------------------------------

Compile the developed indexer project, and obtain the compiled DLL file. Hand over the compiled ``Sample.dll`` file to the 
administrator of the AElf Indexer system. The administrator will place the ``Sample.dll`` file into the ``plugIns`` folder 
within the DApp module of the AElf Indexer system. 

.. code:: bash

    ubuntu@protkey-did-test-indexer-a-01:/opt/aelf-indexer/dapp-bingo/plugins$ ls
    BingoGame.Indexer.CA.dll
    
Subsequently, the AElf Indexer system will automatically initiate the process of pushing blocks to the interface plugin 
for processing, adhering to the pre-subscribed requirements, and simultaneously expose the corresponding GraphQL interfaces 
to external entities. The GraphQL interface address will be ``http://URL:{port}/AElfIndexer_DApp/SampleSchema/graphql``
This playground can check whether the indexer works properly, e.g. The playground for bingogame indexer:

.. image:: playground.png
   :alt: Playground

Conclusion
------------------------------------

By following these steps, DApps can seamlessly integrate with the AElf Indexer, enabling efficient retrieval and processing 
of on-chain data. This comprehensive guide gives introduction and ensures a smooth development process.
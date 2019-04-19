## Enterprise development

This section describes how buisnesses can easily use AElf's framework to build their own chain software. 

One of the driving idea beehind AElf is that it is modular. It is build using software technics that enable reuse and the modules can be picked and configured as fits the requirements. 

We currently use Microsofts NuGet platform as a repository for our modules (see [here](https://www.nuget.org/packages?q=aelf). You can easily pick some of our fundamental building blocks and build further on top of our framework. 

The process you should follow will look something like this:
1. Identify your needs.
2. Identify what problems we already solve - this is what you will reuse.
3. Plan the components you will write yourself.
4. Create the project, use the nuggets of existing modules.
5. Write your own modules.


You can find a simple example [here]
(https://github.com/AElfProject/aelf-examples/tree/demo/chain/mainchain). This repo contains a basic structure showing the basic way to customize our node software.

This page will explain some of the components you'll find in the repository:

#### Minimal program:

The entry point of the program, this is needed in any application.

```csharp
class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging(builder => { builder.ClearProviders(); })
            .UseStartup<Startup>();
}

```
The next step is defining a custom startup class, like this:

```csharp
public class Startup
{
    // ...

    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        AddApplication<MainChainModule>(services);
        return services.BuildAutofacServiceProvider();
    }

    // ...
}
```

Note: this is volontarily simplified to only highlight the most important. For a more complete version, check out the demo.

Here what is important is the ```AddApplication<MainChainModule>(services);``` this line will add the main module, here named **MainChainModule** but you can decide on a more appropriate name. This module is the main application module that brings in all the other dependencies.

The modules is an AElf module, with the following skeleton:

```csharp
[DependsOn(
        typeof(KernelAElfModule),
        typeof(OSAElfModule),
        typeof(NetRpcAElfModule),
        ...
    )]
    public class MainChainModule : AElfModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            // init logic goes here
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            // shutdown logic goes here
        }
    }
```

First notice that the **DependsOn** annotation will bring in other dependencies. The dependencies all have their own module definition, with initialization and shutdown logic if needed.

### Basic modules

### AbpAspNetCoreModule

This is important since our framework partially builds on top of the ABP framework, which itself builds on top of ASP.

#### The Kernel module (KernelAElfModule)

This is one of the main modules that you will probably want to reuse. This module brings in core functionality to work with the chain. It also contains the abstract mechanisms and definitions to deal with smart contracts. Re-writting this is possible but is a considerable effort, so if you decide not to use make sure you have things well planned out.

#### The OS module (OSAElfModule)

The OS module adds on top of the Kernel modules. It mainly defines Networking code and synchronisation event handlers.

#### The C# module (CSharpRuntimeAElfModule)

This modules will bring everything you need to be able to use C# as a programming language.

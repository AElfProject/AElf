```puml

@startuml
package SmartContract.Abstracts.SDK{
    interface ISmartContractSdkService{

    }
}

package SDk.CSharp{

    interface CSharpSmartContract{
            ISmartContractSdkService SmartContractSdkService;
    }

    CSharpSmartContract -> ISmartContractSdkService
    
}
@enduml
```
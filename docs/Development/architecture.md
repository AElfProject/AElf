Hello

```puml
@startuml

package "Application" {
  [Service]
  [Event]
  [Controller]
  


  [Controller] -> [Service] : injection

  [EventHandler] ..> [Event] : subscribe

  [EventHandler] -> [Controller] : injection
  [EventHandler] -> [Service] : injection

  [Service] ..> [Event] : publish
}

package "Domain" {
  [Manager]
  [Context]

  [Entity]

  [FSM]


  [Controller] ..> [Context] : create
  [Service] ..> [Context] : parameters
  [Context] -down-> [FSM]
  [Context] -> [Manager] 


  [Service] -down-> [Manager]

  [Manager] -> [Entity]

note top of [Context]
  context is stateful.
end note
}


@enduml
```
AELF project

```puml
@startuml

package "AElf.Kernel.Application" {
}

package "AElf.Kernel.Domain" {
}


@enduml
```
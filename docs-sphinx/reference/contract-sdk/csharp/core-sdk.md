<a name='assembly'></a>
# AElf.CSharp.Core

## Contents

- [Builder](#T-AElf-CSharp-Core-ServerServiceDefinition-Builder 'AElf.CSharp.Core.ServerServiceDefinition.Builder')
  - [#ctor()](#M-AElf-CSharp-Core-ServerServiceDefinition-Builder-#ctor 'AElf.CSharp.Core.ServerServiceDefinition.Builder.#ctor')
  - [AddMethod\`\`2(method,handler)](#M-AElf-CSharp-Core-ServerServiceDefinition-Builder-AddMethod``2-AElf-CSharp-Core-Method{``0,``1},AElf-CSharp-Core-UnaryServerMethod{``0,``1}- 'AElf.CSharp.Core.ServerServiceDefinition.Builder.AddMethod``2(AElf.CSharp.Core.Method{``0,``1},AElf.CSharp.Core.UnaryServerMethod{``0,``1})')
  - [Build()](#M-AElf-CSharp-Core-ServerServiceDefinition-Builder-Build 'AElf.CSharp.Core.ServerServiceDefinition.Builder.Build')
- [EncodingHelper](#T-AElf-CSharp-Core-Utils-EncodingHelper 'AElf.CSharp.Core.Utils.EncodingHelper')
  - [EncodeUtf8(str)](#M-AElf-CSharp-Core-Utils-EncodingHelper-EncodeUtf8-System-String- 'AElf.CSharp.Core.Utils.EncodingHelper.EncodeUtf8(System.String)')
- [IMethod](#T-AElf-CSharp-Core-IMethod 'AElf.CSharp.Core.IMethod')
  - [FullName](#P-AElf-CSharp-Core-IMethod-FullName 'AElf.CSharp.Core.IMethod.FullName')
  - [Name](#P-AElf-CSharp-Core-IMethod-Name 'AElf.CSharp.Core.IMethod.Name')
  - [ServiceName](#P-AElf-CSharp-Core-IMethod-ServiceName 'AElf.CSharp.Core.IMethod.ServiceName')
  - [Type](#P-AElf-CSharp-Core-IMethod-Type 'AElf.CSharp.Core.IMethod.Type')
- [Marshaller\`1](#T-AElf-CSharp-Core-Marshaller`1 'AElf.CSharp.Core.Marshaller`1')
  - [#ctor(serializer,deserializer)](#M-AElf-CSharp-Core-Marshaller`1-#ctor-System-Func{`0,System-Byte[]},System-Func{System-Byte[],`0}- 'AElf.CSharp.Core.Marshaller`1.#ctor(System.Func{`0,System.Byte[]},System.Func{System.Byte[],`0})')
  - [Deserializer](#P-AElf-CSharp-Core-Marshaller`1-Deserializer 'AElf.CSharp.Core.Marshaller`1.Deserializer')
  - [Serializer](#P-AElf-CSharp-Core-Marshaller`1-Serializer 'AElf.CSharp.Core.Marshaller`1.Serializer')
- [Marshallers](#T-AElf-CSharp-Core-Marshallers 'AElf.CSharp.Core.Marshallers')
  - [StringMarshaller](#P-AElf-CSharp-Core-Marshallers-StringMarshaller 'AElf.CSharp.Core.Marshallers.StringMarshaller')
  - [Create\`\`1()](#M-AElf-CSharp-Core-Marshallers-Create``1-System-Func{``0,System-Byte[]},System-Func{System-Byte[],``0}- 'AElf.CSharp.Core.Marshallers.Create``1(System.Func{``0,System.Byte[]},System.Func{System.Byte[],``0})')
- [MethodType](#T-AElf-CSharp-Core-MethodType 'AElf.CSharp.Core.MethodType')
  - [Action](#F-AElf-CSharp-Core-MethodType-Action 'AElf.CSharp.Core.MethodType.Action')
  - [View](#F-AElf-CSharp-Core-MethodType-View 'AElf.CSharp.Core.MethodType.View')
- [Method\`2](#T-AElf-CSharp-Core-Method`2 'AElf.CSharp.Core.Method`2')
  - [#ctor(type,serviceName,name,requestMarshaller,responseMarshaller)](#M-AElf-CSharp-Core-Method`2-#ctor-AElf-CSharp-Core-MethodType,System-String,System-String,AElf-CSharp-Core-Marshaller{`0},AElf-CSharp-Core-Marshaller{`1}- 'AElf.CSharp.Core.Method`2.#ctor(AElf.CSharp.Core.MethodType,System.String,System.String,AElf.CSharp.Core.Marshaller{`0},AElf.CSharp.Core.Marshaller{`1})')
  - [FullName](#P-AElf-CSharp-Core-Method`2-FullName 'AElf.CSharp.Core.Method`2.FullName')
  - [Name](#P-AElf-CSharp-Core-Method`2-Name 'AElf.CSharp.Core.Method`2.Name')
  - [RequestMarshaller](#P-AElf-CSharp-Core-Method`2-RequestMarshaller 'AElf.CSharp.Core.Method`2.RequestMarshaller')
  - [ResponseMarshaller](#P-AElf-CSharp-Core-Method`2-ResponseMarshaller 'AElf.CSharp.Core.Method`2.ResponseMarshaller')
  - [ServiceName](#P-AElf-CSharp-Core-Method`2-ServiceName 'AElf.CSharp.Core.Method`2.ServiceName')
  - [Type](#P-AElf-CSharp-Core-Method`2-Type 'AElf.CSharp.Core.Method`2.Type')
  - [GetFullName()](#M-AElf-CSharp-Core-Method`2-GetFullName-System-String,System-String- 'AElf.CSharp.Core.Method`2.GetFullName(System.String,System.String)')
- [Preconditions](#T-AElf-CSharp-Core-Utils-Preconditions 'AElf.CSharp.Core.Utils.Preconditions')
  - [CheckNotNull\`\`1(reference)](#M-AElf-CSharp-Core-Utils-Preconditions-CheckNotNull``1-``0- 'AElf.CSharp.Core.Utils.Preconditions.CheckNotNull``1(``0)')
  - [CheckNotNull\`\`1(reference,paramName)](#M-AElf-CSharp-Core-Utils-Preconditions-CheckNotNull``1-``0,System-String- 'AElf.CSharp.Core.Utils.Preconditions.CheckNotNull``1(``0,System.String)')
- [SafeMath](#T-AElf-CSharp-Core-SafeMath 'AElf.CSharp.Core.SafeMath')
- [ServerServiceDefinition](#T-AElf-CSharp-Core-ServerServiceDefinition 'AElf.CSharp.Core.ServerServiceDefinition')
  - [BindService()](#M-AElf-CSharp-Core-ServerServiceDefinition-BindService-AElf-CSharp-Core-ServiceBinderBase- 'AElf.CSharp.Core.ServerServiceDefinition.BindService(AElf.CSharp.Core.ServiceBinderBase)')
  - [CreateBuilder()](#M-AElf-CSharp-Core-ServerServiceDefinition-CreateBuilder 'AElf.CSharp.Core.ServerServiceDefinition.CreateBuilder')
- [ServiceBinderBase](#T-AElf-CSharp-Core-ServiceBinderBase 'AElf.CSharp.Core.ServiceBinderBase')
  - [AddMethod\`\`2(method,handler)](#M-AElf-CSharp-Core-ServiceBinderBase-AddMethod``2-AElf-CSharp-Core-Method{``0,``1},AElf-CSharp-Core-UnaryServerMethod{``0,``1}- 'AElf.CSharp.Core.ServiceBinderBase.AddMethod``2(AElf.CSharp.Core.Method{``0,``1},AElf.CSharp.Core.UnaryServerMethod{``0,``1})')
- [TimestampExtensions](#T-AElf-CSharp-Core-Extension-TimestampExtensions 'AElf.CSharp.Core.Extension.TimestampExtensions')
  - [AddDays(timestamp,days)](#M-AElf-CSharp-Core-Extension-TimestampExtensions-AddDays-Google-Protobuf-WellKnownTypes-Timestamp,System-Int64- 'AElf.CSharp.Core.Extension.TimestampExtensions.AddDays(Google.Protobuf.WellKnownTypes.Timestamp,System.Int64)')
  - [AddHours(timestamp,hours)](#M-AElf-CSharp-Core-Extension-TimestampExtensions-AddHours-Google-Protobuf-WellKnownTypes-Timestamp,System-Int64- 'AElf.CSharp.Core.Extension.TimestampExtensions.AddHours(Google.Protobuf.WellKnownTypes.Timestamp,System.Int64)')
  - [AddMilliseconds(timestamp,milliseconds)](#M-AElf-CSharp-Core-Extension-TimestampExtensions-AddMilliseconds-Google-Protobuf-WellKnownTypes-Timestamp,System-Int64- 'AElf.CSharp.Core.Extension.TimestampExtensions.AddMilliseconds(Google.Protobuf.WellKnownTypes.Timestamp,System.Int64)')
  - [AddMinutes(timestamp,minutes)](#M-AElf-CSharp-Core-Extension-TimestampExtensions-AddMinutes-Google-Protobuf-WellKnownTypes-Timestamp,System-Int64- 'AElf.CSharp.Core.Extension.TimestampExtensions.AddMinutes(Google.Protobuf.WellKnownTypes.Timestamp,System.Int64)')
  - [AddSeconds(timestamp,seconds)](#M-AElf-CSharp-Core-Extension-TimestampExtensions-AddSeconds-Google-Protobuf-WellKnownTypes-Timestamp,System-Int64- 'AElf.CSharp.Core.Extension.TimestampExtensions.AddSeconds(Google.Protobuf.WellKnownTypes.Timestamp,System.Int64)')
  - [Max(timestamp1,timestamp2)](#M-AElf-CSharp-Core-Extension-TimestampExtensions-Max-Google-Protobuf-WellKnownTypes-Timestamp,Google-Protobuf-WellKnownTypes-Timestamp- 'AElf.CSharp.Core.Extension.TimestampExtensions.Max(Google.Protobuf.WellKnownTypes.Timestamp,Google.Protobuf.WellKnownTypes.Timestamp)')
  - [Milliseconds(duration)](#M-AElf-CSharp-Core-Extension-TimestampExtensions-Milliseconds-Google-Protobuf-WellKnownTypes-Duration- 'AElf.CSharp.Core.Extension.TimestampExtensions.Milliseconds(Google.Protobuf.WellKnownTypes.Duration)')
- [UnaryServerMethod\`2](#T-AElf-CSharp-Core-UnaryServerMethod`2 'AElf.CSharp.Core.UnaryServerMethod`2')

<a name='T-AElf-CSharp-Core-ServerServiceDefinition-Builder'></a>
## Builder `type`

##### Namespace

AElf.CSharp.Core.ServerServiceDefinition

##### Summary

Builder class for [ServerServiceDefinition](#T-AElf-CSharp-Core-ServerServiceDefinition 'AElf.CSharp.Core.ServerServiceDefinition').

<a name='M-AElf-CSharp-Core-ServerServiceDefinition-Builder-#ctor'></a>
### #ctor() `constructor`

##### Summary

Creates a new instance of builder.

##### Parameters

This constructor has no parameters.

<a name='M-AElf-CSharp-Core-ServerServiceDefinition-Builder-AddMethod``2-AElf-CSharp-Core-Method{``0,``1},AElf-CSharp-Core-UnaryServerMethod{``0,``1}-'></a>
### AddMethod\`\`2(method,handler) `method`

##### Summary

Adds a definition for a single request - single response method.

##### Returns

This builder instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| method | [AElf.CSharp.Core.Method{\`\`0,\`\`1}](#T-AElf-CSharp-Core-Method{``0,``1} 'AElf.CSharp.Core.Method{``0,``1}') | The method. |
| handler | [AElf.CSharp.Core.UnaryServerMethod{\`\`0,\`\`1}](#T-AElf-CSharp-Core-UnaryServerMethod{``0,``1} 'AElf.CSharp.Core.UnaryServerMethod{``0,``1}') | The method handler. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TRequest | The request message class. |
| TResponse | The response message class. |

<a name='M-AElf-CSharp-Core-ServerServiceDefinition-Builder-Build'></a>
### Build() `method`

##### Summary

Creates an immutable `ServerServiceDefinition` from this builder.

##### Returns

The `ServerServiceDefinition` object.

##### Parameters

This method has no parameters.

<a name='T-AElf-CSharp-Core-Utils-EncodingHelper'></a>
## EncodingHelper `type`

##### Namespace

AElf.CSharp.Core.Utils

##### Summary

Helper class for serializing strings.

<a name='M-AElf-CSharp-Core-Utils-EncodingHelper-EncodeUtf8-System-String-'></a>
### EncodeUtf8(str) `method`

##### Summary

Serializes a UTF-8 string to a byte array.

##### Returns

the serialized string.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| str | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

<a name='T-AElf-CSharp-Core-IMethod'></a>
## IMethod `type`

##### Namespace

AElf.CSharp.Core

##### Summary

A non-generic representation of a remote method.

<a name='P-AElf-CSharp-Core-IMethod-FullName'></a>
### FullName `property`

##### Summary

Gets the fully qualified name of the method. On the server side, methods are dispatched
based on this name.

<a name='P-AElf-CSharp-Core-IMethod-Name'></a>
### Name `property`

##### Summary

Gets the unqualified name of the method.

<a name='P-AElf-CSharp-Core-IMethod-ServiceName'></a>
### ServiceName `property`

##### Summary

Gets the name of the service to which this method belongs.

<a name='P-AElf-CSharp-Core-IMethod-Type'></a>
### Type `property`

##### Summary

Gets the type of the method.

<a name='T-AElf-CSharp-Core-Marshaller`1'></a>
## Marshaller\`1 `type`

##### Namespace

AElf.CSharp.Core

##### Summary

Encapsulates the logic for serializing and deserializing messages.

<a name='M-AElf-CSharp-Core-Marshaller`1-#ctor-System-Func{`0,System-Byte[]},System-Func{System-Byte[],`0}-'></a>
### #ctor(serializer,deserializer) `constructor`

##### Summary

Initializes a new marshaller from simple serialize/deserialize functions.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| serializer | [System.Func{\`0,System.Byte[]}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{`0,System.Byte[]}') | Function that will be used to serialize messages. |
| deserializer | [System.Func{System.Byte[],\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{System.Byte[],`0}') | Function that will be used to deserialize messages. |

<a name='P-AElf-CSharp-Core-Marshaller`1-Deserializer'></a>
### Deserializer `property`

##### Summary

Gets the deserializer function.

<a name='P-AElf-CSharp-Core-Marshaller`1-Serializer'></a>
### Serializer `property`

##### Summary

Gets the serializer function.

<a name='T-AElf-CSharp-Core-Marshallers'></a>
## Marshallers `type`

##### Namespace

AElf.CSharp.Core

##### Summary

Utilities for creating marshallers.

<a name='P-AElf-CSharp-Core-Marshallers-StringMarshaller'></a>
### StringMarshaller `property`

##### Summary

Returns a marshaller for `string` type. This is useful for testing.

<a name='M-AElf-CSharp-Core-Marshallers-Create``1-System-Func{``0,System-Byte[]},System-Func{System-Byte[],``0}-'></a>
### Create\`\`1() `method`

##### Summary

Creates a marshaller from specified serializer and deserializer.

##### Parameters

This method has no parameters.

<a name='T-AElf-CSharp-Core-MethodType'></a>
## MethodType `type`

##### Namespace

AElf.CSharp.Core

<a name='F-AElf-CSharp-Core-MethodType-Action'></a>
### Action `constants`

##### Summary

The method modifies the contrac state.

<a name='F-AElf-CSharp-Core-MethodType-View'></a>
### View `constants`

##### Summary

The method doesn't modify the contract state.

<a name='T-AElf-CSharp-Core-Method`2'></a>
## Method\`2 `type`

##### Namespace

AElf.CSharp.Core

##### Summary

A description of a remote method.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TRequest | Request message type for this method. |
| TResponse | Response message type for this method. |

<a name='M-AElf-CSharp-Core-Method`2-#ctor-AElf-CSharp-Core-MethodType,System-String,System-String,AElf-CSharp-Core-Marshaller{`0},AElf-CSharp-Core-Marshaller{`1}-'></a>
### #ctor(type,serviceName,name,requestMarshaller,responseMarshaller) `constructor`

##### Summary

Initializes a new instance of the `Method` class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| type | [AElf.CSharp.Core.MethodType](#T-AElf-CSharp-Core-MethodType 'AElf.CSharp.Core.MethodType') | Type of method. |
| serviceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Name of service this method belongs to. |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Unqualified name of the method. |
| requestMarshaller | [AElf.CSharp.Core.Marshaller{\`0}](#T-AElf-CSharp-Core-Marshaller{`0} 'AElf.CSharp.Core.Marshaller{`0}') | Marshaller used for request messages. |
| responseMarshaller | [AElf.CSharp.Core.Marshaller{\`1}](#T-AElf-CSharp-Core-Marshaller{`1} 'AElf.CSharp.Core.Marshaller{`1}') | Marshaller used for response messages. |

<a name='P-AElf-CSharp-Core-Method`2-FullName'></a>
### FullName `property`

##### Summary

Gets the fully qualified name of the method. On the server side, methods are dispatched
based on this name.

<a name='P-AElf-CSharp-Core-Method`2-Name'></a>
### Name `property`

##### Summary

Gets the unqualified name of the method.

<a name='P-AElf-CSharp-Core-Method`2-RequestMarshaller'></a>
### RequestMarshaller `property`

##### Summary

Gets the marshaller used for request messages.

<a name='P-AElf-CSharp-Core-Method`2-ResponseMarshaller'></a>
### ResponseMarshaller `property`

##### Summary

Gets the marshaller used for response messages.

<a name='P-AElf-CSharp-Core-Method`2-ServiceName'></a>
### ServiceName `property`

##### Summary

Gets the name of the service to which this method belongs.

<a name='P-AElf-CSharp-Core-Method`2-Type'></a>
### Type `property`

##### Summary

Gets the type of the method.

<a name='M-AElf-CSharp-Core-Method`2-GetFullName-System-String,System-String-'></a>
### GetFullName() `method`

##### Summary

Gets full name of the method including the service name.

##### Parameters

This method has no parameters.

<a name='T-AElf-CSharp-Core-Utils-Preconditions'></a>
## Preconditions `type`

##### Namespace

AElf.CSharp.Core.Utils

<a name='M-AElf-CSharp-Core-Utils-Preconditions-CheckNotNull``1-``0-'></a>
### CheckNotNull\`\`1(reference) `method`

##### Summary

Throws [ArgumentNullException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentNullException 'System.ArgumentNullException') if reference is null.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| reference | [\`\`0](#T-``0 '``0') | The reference. |

<a name='M-AElf-CSharp-Core-Utils-Preconditions-CheckNotNull``1-``0,System-String-'></a>
### CheckNotNull\`\`1(reference,paramName) `method`

##### Summary

Throws [ArgumentNullException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentNullException 'System.ArgumentNullException') if reference is null.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| reference | [\`\`0](#T-``0 '``0') | The reference. |
| paramName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The parameter name. |

<a name='T-AElf-CSharp-Core-SafeMath'></a>
## SafeMath `type`

##### Namespace

AElf.CSharp.Core

##### Summary

Helper methods for safe math operations that explicitly check for overflow.

<a name='T-AElf-CSharp-Core-ServerServiceDefinition'></a>
## ServerServiceDefinition `type`

##### Namespace

AElf.CSharp.Core

##### Summary

Stores mapping of methods to server call handlers.
Normally, the `ServerServiceDefinition` objects will be created by the `BindService` factory method 
that is part of the autogenerated code for a protocol buffers service definition.

<a name='M-AElf-CSharp-Core-ServerServiceDefinition-BindService-AElf-CSharp-Core-ServiceBinderBase-'></a>
### BindService() `method`

##### Summary

Forwards all the previously stored `AddMethod` calls to the service binder.

##### Parameters

This method has no parameters.

<a name='M-AElf-CSharp-Core-ServerServiceDefinition-CreateBuilder'></a>
### CreateBuilder() `method`

##### Summary

Creates a new builder object for `ServerServiceDefinition`.

##### Returns

The builder object.

##### Parameters

This method has no parameters.

<a name='T-AElf-CSharp-Core-ServiceBinderBase'></a>
## ServiceBinderBase `type`

##### Namespace

AElf.CSharp.Core

##### Summary

Allows binding server-side method implementations in alternative serving stacks.
Instances of this class are usually populated by the `BindService` method
that is part of the autogenerated code for a protocol buffers service definition.

<a name='M-AElf-CSharp-Core-ServiceBinderBase-AddMethod``2-AElf-CSharp-Core-Method{``0,``1},AElf-CSharp-Core-UnaryServerMethod{``0,``1}-'></a>
### AddMethod\`\`2(method,handler) `method`

##### Summary

Adds a definition for a single request - single response method.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| method | [AElf.CSharp.Core.Method{\`\`0,\`\`1}](#T-AElf-CSharp-Core-Method{``0,``1} 'AElf.CSharp.Core.Method{``0,``1}') | The method. |
| handler | [AElf.CSharp.Core.UnaryServerMethod{\`\`0,\`\`1}](#T-AElf-CSharp-Core-UnaryServerMethod{``0,``1} 'AElf.CSharp.Core.UnaryServerMethod{``0,``1}') | The method handler. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TRequest | The request message class. |
| TResponse | The response message class. |

<a name='T-AElf-CSharp-Core-Extension-TimestampExtensions'></a>
## TimestampExtensions `type`

##### Namespace

AElf.CSharp.Core.Extension

##### Summary

Helper methods for dealing with protobuf timestamps.

<a name='M-AElf-CSharp-Core-Extension-TimestampExtensions-AddDays-Google-Protobuf-WellKnownTypes-Timestamp,System-Int64-'></a>
### AddDays(timestamp,days) `method`

##### Summary

Adds a given amount of days to a timestamp. Returns a new instance.

##### Returns

a new timestamp instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| timestamp | [Google.Protobuf.WellKnownTypes.Timestamp](#T-Google-Protobuf-WellKnownTypes-Timestamp 'Google.Protobuf.WellKnownTypes.Timestamp') | the timestamp. |
| days | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | the amount of days. |

<a name='M-AElf-CSharp-Core-Extension-TimestampExtensions-AddHours-Google-Protobuf-WellKnownTypes-Timestamp,System-Int64-'></a>
### AddHours(timestamp,hours) `method`

##### Summary

Adds a given amount of hours to a timestamp. Returns a new instance.

##### Returns

a new timestamp instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| timestamp | [Google.Protobuf.WellKnownTypes.Timestamp](#T-Google-Protobuf-WellKnownTypes-Timestamp 'Google.Protobuf.WellKnownTypes.Timestamp') | the timestamp. |
| hours | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | the amount of hours. |

<a name='M-AElf-CSharp-Core-Extension-TimestampExtensions-AddMilliseconds-Google-Protobuf-WellKnownTypes-Timestamp,System-Int64-'></a>
### AddMilliseconds(timestamp,milliseconds) `method`

##### Summary

Adds a given amount of milliseconds to a timestamp. Returns a new instance.

##### Returns

a new timestamp instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| timestamp | [Google.Protobuf.WellKnownTypes.Timestamp](#T-Google-Protobuf-WellKnownTypes-Timestamp 'Google.Protobuf.WellKnownTypes.Timestamp') | the timestamp. |
| milliseconds | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | the amount of milliseconds to add. |

<a name='M-AElf-CSharp-Core-Extension-TimestampExtensions-AddMinutes-Google-Protobuf-WellKnownTypes-Timestamp,System-Int64-'></a>
### AddMinutes(timestamp,minutes) `method`

##### Summary

Adds a given amount of minutes to a timestamp. Returns a new instance.

##### Returns

a new timestamp instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| timestamp | [Google.Protobuf.WellKnownTypes.Timestamp](#T-Google-Protobuf-WellKnownTypes-Timestamp 'Google.Protobuf.WellKnownTypes.Timestamp') | the timestamp. |
| minutes | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | the amount of minutes. |

<a name='M-AElf-CSharp-Core-Extension-TimestampExtensions-AddSeconds-Google-Protobuf-WellKnownTypes-Timestamp,System-Int64-'></a>
### AddSeconds(timestamp,seconds) `method`

##### Summary

Adds a given amount of seconds to a timestamp. Returns a new instance.

##### Returns

a new timestamp instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| timestamp | [Google.Protobuf.WellKnownTypes.Timestamp](#T-Google-Protobuf-WellKnownTypes-Timestamp 'Google.Protobuf.WellKnownTypes.Timestamp') | the timestamp. |
| seconds | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | the amount of seconds. |

<a name='M-AElf-CSharp-Core-Extension-TimestampExtensions-Max-Google-Protobuf-WellKnownTypes-Timestamp,Google-Protobuf-WellKnownTypes-Timestamp-'></a>
### Max(timestamp1,timestamp2) `method`

##### Summary

Compares two timestamps and returns the greater one.

##### Returns

the greater timestamp.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| timestamp1 | [Google.Protobuf.WellKnownTypes.Timestamp](#T-Google-Protobuf-WellKnownTypes-Timestamp 'Google.Protobuf.WellKnownTypes.Timestamp') | the first timestamp |
| timestamp2 | [Google.Protobuf.WellKnownTypes.Timestamp](#T-Google-Protobuf-WellKnownTypes-Timestamp 'Google.Protobuf.WellKnownTypes.Timestamp') | the second timestamp |

<a name='M-AElf-CSharp-Core-Extension-TimestampExtensions-Milliseconds-Google-Protobuf-WellKnownTypes-Duration-'></a>
### Milliseconds(duration) `method`

##### Summary

Converts a protobuf duration to long.

##### Returns

the duration represented with a long.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| duration | [Google.Protobuf.WellKnownTypes.Duration](#T-Google-Protobuf-WellKnownTypes-Duration 'Google.Protobuf.WellKnownTypes.Duration') | the duration to convert. |

<a name='T-AElf-CSharp-Core-UnaryServerMethod`2'></a>
## UnaryServerMethod\`2 `type`

##### Namespace

AElf.CSharp.Core

##### Summary

Handler for a contract method.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TRequest | Request message type for this method. |
| TResponse | Response message type for this method. |

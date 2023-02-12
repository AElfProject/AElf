AElf.CSharp.Core
================


Contents
--------

-  :ref:`Builder <AElf-CSharp-Core-ServerServiceDefinition-Builder>`

   -  :ref:`ctor() <AElf-CSharp-Core-ServerServiceDefinition-Builder-ctor>`
   -  :ref:`AddMethod(method,handler) <AElf-CSharp-Core-ServerServiceDefinition-Builder-AddMethod-AElf-CSharp-Core-Method-AElf-CSharp-Core-UnaryServerMethod>`
   -  :ref:`Build() <AElf-CSharp-Core-ServerServiceDefinition-Builder-Build>`

-  :ref:`EncodingHelper <AElf-CSharp-Core-Utils-EncodingHelper>`

   -  :ref:`EncodeUtf8(str) <AElf-CSharp-Core-Utils-EncodingHelper-EncodeUtf8-System-String>`

-  :ref:`IMethod <AElf-CSharp-Core-IMethod>`

   -  :ref:`FullName <AElf-CSharp-Core-IMethod-FullName>`
   -  :ref:`Name <AElf-CSharp-Core-IMethod-Name>`
   -  :ref:`ServiceName <AElf-CSharp-Core-IMethod-ServiceName>`
   -  :ref:`Type <AElf-CSharp-Core-IMethod-Typer>`

-  :ref:`Marshaller <AElf-CSharp-Core-Marshaller>`

   -  :ref:`ctor(serializer,deserializer) <AElf-CSharp-Core-Marshaller-ctor-System-Func-System-Byte-System-Func-System-Byte>`
   -  :ref:`Deserializer <AElf-CSharp-Core-Marshaller-Deserializer>`
   -  :ref:`Serializer <AElf-CSharp-Core-Marshaller-Serializer>`

-  :ref:`Marshallers <AElf-CSharp-Core-Marshallers>`

   -  :ref:`StringMarshaller <AElf-CSharp-Core-Marshallers-StringMarshaller>`
   -  :ref:`Create() <AElf-CSharp-Core-Marshallers-Create>`

-  :ref:`MethodType <AElf-CSharp-Core-MethodType>`

   -  :ref:`Action <AElf-CSharp-Core-MethodType-Action>`
   -  :ref:`View <AElf-CSharp-Core-MethodType-View>`

-  :ref:`Method <AElf-CSharp-Core-Method>`

   -  :ref:`ctor(type,serviceName,name,requestMarshaller,responseMarshaller) <AElf-CSharp-Core-Method-ctor-AElf-CSharp-Core-MethodType-System-String-System-String-AElf-CSharp-Core-Marshaller-AElf-CSharp-Core-Marshaller>`
   -  :ref:`FullName <AElf-CSharp-Core-Method-FullName>`
   -  :ref:`Name <AElf-CSharp-Core-Method-Name>`
   -  :ref:`RequestMarshaller <AElf-CSharp-Core-Method-RequestMarshaller>`
   -  :ref:`ResponseMarshaller <AElf-CSharp-Core-Method-ResponseMarshaller>`
   -  :ref:`ServiceName <AElf-CSharp-Core-Method-ServiceName>`
   -  :ref:`Type <AElf-CSharp-Core-Method-Type>`
   -  :ref:`GetFullName() <AElf-CSharp-Core-Method-GetFullName-System-String-System-String>`

-  :ref:`Preconditions <AElf-CSharp-Core-Utils-Preconditions>`

   -  :ref:`CheckNotNull(reference) <AElf-CSharp-Core-Utils-Preconditions-CheckNotNull>`
   -  :ref:`CheckNotNullreference,paramName) <AElf-CSharp-Core-Utils-Preconditions-CheckNotNull-System-String>`

-  :ref:`SafeMath <AElf-CSharp-Core-SafeMath>`
-  :ref:`ServerServiceDefinition <AElf-CSharp-Core-ServerServiceDefinition>`

   -  :ref:`BindService() <AElf-CSharp-Core-ServerServiceDefinition-BindService-AElf-CSharp-Core-ServiceBinderBase>`
   -  :ref:`CreateBuilder() <AElf-CSharp-Core-ServerServiceDefinition-CreateBuilder>`

-  :ref:`ServiceBinderBase <AElf-CSharp-Core-ServiceBinderBase>`

   -  :ref:`AddMethod(method,handler) <AElf-CSharp-Core-ServiceBinderBase-AddMethod-AElf-CSharp-Core-Method-AElf-CSharp-Core-UnaryServerMethod>`

-  :ref:`TimestampExtensions <AElf-CSharp-Core-Extension-TimestampExtensions>`

   -  :ref:`AddDays(timestamp,days) <AElf-CSharp-Core-Extension-TimestampExtensions-AddDays-Google-Protobuf-WellKnownTypes-Timestamp-System-Int64>`
   -  :ref:`AddHours(timestamp,hours) <AElf-CSharp-Core-Extension-TimestampExtensions-AddHours-Google-Protobuf-WellKnownTypes-Timestamp-System-Int64>`
   -  :ref:`AddMilliseconds(timestamp,milliseconds) <AElf-CSharp-Core-Extension-TimestampExtensions-AddMilliseconds-Google-Protobuf-WellKnownTypes-Timestamp-System-Int64>`
   -  :ref:`AddMinutes(timestamp,minutes) <AElf-CSharp-Core-Extension-TimestampExtensions-AddMinutes-Google-Protobuf-WellKnownTypes-Timestamp-System-Int64>`
   -  :ref:`AddSeconds(timestamp,seconds) <AElf-CSharp-Core-Extension-TimestampExtensions-AddSeconds-Google-Protobuf-WellKnownTypes-Timestamp-System-Int64>`
   -  :ref:`Max(timestamp1,timestamp2) <AElf-CSharp-Core-Extension-TimestampExtensions-Max-Google-Protobuf-WellKnownTypes-Timestamp-Google-Protobuf-WellKnownTypes-Timestamp>`
   -  :ref:`Milliseconds(duration) <AElf-CSharp-Core-Extension-TimestampExtensions-Milliseconds-Google-Protobuf-WellKnownTypes-Duration>`

-  :ref:`UnaryServerMethod <AElf-CSharp-Core-UnaryServerMethod>`


.. _AElf-CSharp-Core-ServerServiceDefinition-Builder:

Builder ``type``
>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core.ServerServiceDefinition

Summary
'''''''

Builder class for :ref:`ServerServiceDefinition <AElf-CSharp-Core-ServerServiceDefinition>`.

.. _AElf-CSharp-Core-ServerServiceDefinition-Builder-ctor:

ctor() ``constructor``
>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Creates a new instance of builder.

Parameters
''''''''''

This constructor has no parameters.

.. _AElf-CSharp-Core-ServerServiceDefinition-Builder-AddMethod-AElf-CSharp-Core-Method-AElf-CSharp-Core-UnaryServerMethod:

AddMethod``2(method,handler) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Adds a definition for a single request - single response method.

Returns
'''''''

This builder instance.

Parameters
''''''''''

+---------+------------------------------------+---------------------+
| Name    | Type                               | Description         |
+=========+====================================+=====================+
| method  | :ref:`AElf.CSharp.Core.Method <AEl\| The method.         |
|         | f-CSharp-Core-Method>`             |                     |
+---------+------------------------------------+---------------------+
| handler | :ref:`AElf.CSharp.Core.UnaryServer\| The method handler. |
|         | Method <AElf-CSharp-Core-UnaryServ\|                     |
|         | erMethod>`                         |                     |
+---------+------------------------------------+---------------------+

Generic Types
'''''''''''''

========= ===========================
Name      Description
========= ===========================
TRequest  The request message class.
TResponse The response message class.
========= ===========================

.. _AElf-CSharp-Core-ServerServiceDefinition-Builder-Build:

Build() ``method``
>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Creates an immutable ``ServerServiceDefinition`` from this builder.

Returns
'''''''

The ``ServerServiceDefinition`` object.

Parameters
''''''''''

This method has no parameters.

.. _AElf-CSharp-Core-Utils-EncodingHelper:

EncodingHelper ``type``
>>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core.Utils

Summary
'''''''

Helper class for serializing strings.

.. _AElf-CSharp-Core-Utils-EncodingHelper-EncodeUtf8-System-String:

EncodeUtf8(str) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Serializes a UTF-8 string to a byte array.

Returns
'''''''

the serialized string.

Parameters
''''''''''

+------+-----------------------------------------------+-------------+
| Name | Type                                          | Description |
+======+===============================================+=============+
| str  | `System.String <http://msdn.microsoft.com/que |             |
|      | ry/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:S |             |
|      | ystem.String>`__                              |             |
+------+-----------------------------------------------+-------------+

.. _AElf-CSharp-Core-IMethod:

IMethod ``type``
>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core

Summary
'''''''

A non-generic representation of a remote method.

.. _AElf-CSharp-Core-IMethod-FullName:

FullName ``property``
>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the fully qualified name of the method. On the server side, methods
are dispatched based on this name.

.. _AElf-CSharp-Core-IMethod-Name:

Name ``property``
>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the unqualified name of the method.

.. _AElf-CSharp-Core-IMethod-ServiceName:

ServiceName ``property``
>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the name of the service to which this method belongs.

.. _AElf-CSharp-Core-IMethod-Typer:

Type ``property``
>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the type of the method.

.. _AElf-CSharp-Core-Marshaller:

Marshaller ``type``
>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core

Summary
'''''''

Encapsulates the logic for serializing and deserializing messages.

.. _AElf-CSharp-Core-Marshaller-ctor-System-Func-System-Byte-System-Func-System-Byte:

ctor(serializer,deserializer) ``constructor``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Initializes a new marshaller from simple serialize/deserialize
functions.

Parameters
''''''''''

=========== ================================================================================================ ===================================
Name                                                    Type                                                                     Description
=========== ================================================================================================ ===================================
serializer    `System.Func <https://docs.microsoft.com/en-us/dotnet/api/system.func-1?view=netcore-6.0>`__              Function that will be used to
                                                                                                                        deserialize messages.
=========== ================================================================================================ ===================================

.. _AElf-CSharp-Core-Marshaller-Deserializer:

Deserializer ``property``
>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the deserializer function.

.. _AElf-CSharp-Core-Marshaller-Serializer:

Serializer ``property``
>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the serializer function.

.. _AElf-CSharp-Core-Marshallers:

Marshallers ``type``
>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core

Summary
'''''''

Utilities for creating marshallers.

.. _AElf-CSharp-Core-Marshallers-StringMarshaller:

StringMarshaller ``property``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Returns a marshaller for ``string`` type. This is useful for testing.

.. _AElf-CSharp-Core-Marshallers-Create:

Create() ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Creates a marshaller from specified serializer and deserializer.

Parameters
''''''''''

This method has no parameters.

.. _AElf-CSharp-Core-MethodType:

MethodType ``type``
>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core

.. _AElf-CSharp-Core-MethodType-Action:

Action ``constants``
>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

The method modifies the contrac state.

.. _AElf-CSharp-Core-MethodType-View:

View ``constants``
>>>>>>>>>>>>>>>>>>

Summary
'''''''

The method doesn’t modify the contract state.

.. _AElf-CSharp-Core-Method:

Method ``type``
>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core

Summary
'''''''

A description of a remote method.

Generic Types
'''''''''''''

========= ======================================
Name      Description
========= ======================================
TRequest  Request message type for this method.
TResponse Response message type for this method.
========= ======================================

.. _AElf-CSharp-Core-Method-ctor-AElf-CSharp-Core-MethodType-System-String-System-String-AElf-CSharp-Core-Marshaller-AElf-CSharp-Core-Marshaller:

ctor(type,serviceName,name,requestMarshaller,responseMarshaller) ``constructor``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Initializes a new instance of the ``Method`` class.

Parameters
''''''''''

+--------------+----------------+----------------------------------------+
| Name         | Type           | Description                            |
+==============+================+========================================+
| type         | :ref:`AElf.CSh\| Type of method.                        |
|              | arp.Core.Metho\|                                        |
|              | d <AElf-CSharp\|                                        |
|              | -Core-Method>` |                                        |
+--------------+----------------+----------------------------------------+
| serviceName  | `System.String | Name of service this method belongs    |
|              | <http:/        | to.                                    |
|              | /msdn.micros   |                                        |
|              | oft.com/quer   |                                        |
|              | y/dev14.quer   |                                        |
|              | y?appId=Dev1   |                                        |
|              | 4IDEF1&l=EN-   |                                        |
|              | US&k=k:Syste   |                                        |
|              | m.String>`__   |                                        |
+--------------+----------------+----------------------------------------+
| name         | `System.String | Unqualified name of the method.        |
|              | <http:/        |                                        |
|              | /msdn.micros   |                                        |
|              | oft.com/quer   |                                        |
|              | y/dev14.quer   |                                        |
|              | y?appId=Dev1   |                                        |
|              | 4IDEF1&l=EN-   |                                        |
|              | US&k=k:Syste   |                                        |
|              | m.String>`__   |                                        |
+--------------+----------------+----------------------------------------+
| request      | :ref:`AElf.CSh\| Marshaller used for request messages.  |
| Marshaller   | arp.Core.Marsh\|                                        |
|              | aller <AElf-CS\|                                        |
|              | harp-Core-Mars\|                                        |
|              | haller>`       |                                        |
+--------------+----------------+----------------------------------------+
| response     | :ref:`AElf.CSh\| Marshaller used for response messages. |
| Marshaller   | arp.Core.Marsh\|                                        |
|              | aller <AElf-CS\|                                        |
|              | harp-Core-Mars\|                                        |
|              | haller>`       |                                        |
+--------------+----------------+----------------------------------------+

.. _AElf-CSharp-Core-Method-FullName:

FullName ``property``
>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the fully qualified name of the method. On the server side, methods
are dispatched based on this name.

.. _AElf-CSharp-Core-Method-Name:

Name ``property``
>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the unqualified name of the method.

.. _AElf-CSharp-Core-Method-RequestMarshaller:

RequestMarshaller ``property``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the marshaller used for request messages.

.. _AElf-CSharp-Core-Method-ResponseMarshaller:

ResponseMarshaller ``property``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the marshaller used for response messages.

.. _AElf-CSharp-Core-Method-ServiceName:

ServiceName ``property``
>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the name of the service to which this method belongs.

.. _AElf-CSharp-Core-Method-Type:

Type ``property``
>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets the type of the method.

.. _AElf-CSharp-Core-Method-GetFullName-System-String-System-String:

GetFullName() ``method``
>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets full name of the method including the service name.

Parameters
''''''''''

This method has no parameters.

.. _AElf-CSharp-Core-Utils-Preconditions:

Preconditions ``type``
>>>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core.Utils

.. _AElf-CSharp-Core-Utils-Preconditions-CheckNotNull:

CheckNotNull(reference) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Throws
`ArgumentNullException <https://docs.microsoft.com/en-us/dotnet/api/system.argumentnullexception?redirectedfrom=MSDN&view=netframework-4.7.2>`__
if reference is null.

Parameters
''''''''''

========= ===================== ==============
Name      Type                  Description
========= ===================== ==============
reference                       The reference.
========= ===================== ==============

.. _AElf-CSharp-Core-Utils-Preconditions-CheckNotNull-System-String:

CheckNotNull(reference,paramName) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Throws
`ArgumentNullException <https://docs.microsoft.com/en-us/dotnet/api/system.argumentnullexception?redirectedfrom=MSDN&view=netframework-4.7.2>`__
if reference is null.

Parameters
''''''''''

+-----------+----------------------------------+---------------------+
| Name      | Type                             | Description         |
+===========+==================================+=====================+
| reference |                                  | The reference.      |
+-----------+----------------------------------+---------------------+
| paramName | `System.String <http://msdn.micr | The parameter name. |
|           | osoft.com/query/dev14.query?appI |                     |
|           | d=Dev14IDEF1&l=EN-US&k=k:System. |                     |
|           | String>`__                       |                     |
+-----------+----------------------------------+---------------------+

.. _AElf-CSharp-Core-SafeMath:

SafeMath ``type``
>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core

Summary
'''''''

Helper methods for safe math operations that explicitly check for
overflow.

.. _AElf-CSharp-Core-ServerServiceDefinition:

ServerServiceDefinition ``type``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core

Summary
'''''''

Stores mapping of methods to server call handlers. Normally, the
``ServerServiceDefinition`` objects will be created by the
``BindService`` factory method that is part of the autogenerated code
for a protocol buffers service definition.

.. _AElf-CSharp-Core-ServerServiceDefinition-BindService-AElf-CSharp-Core-ServiceBinderBase:

BindService() ``method``
>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Forwards all the previously stored ``AddMethod`` calls to the service
binder.

Parameters
''''''''''

This method has no parameters.

.. _AElf-CSharp-Core-ServerServiceDefinition-CreateBuilder:

CreateBuilder() ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Creates a new builder object for ``ServerServiceDefinition``.

Returns
'''''''

The builder object.

Parameters
''''''''''

This method has no parameters.

.. _AElf-CSharp-Core-ServiceBinderBase:

ServiceBinderBase ``type``
>>>>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core

Summary
'''''''

Allows binding server-side method implementations in alternative serving
stacks. Instances of this class are usually populated by the
``BindService`` method that is part of the autogenerated code for a
protocol buffers service definition.

.. _AElf-CSharp-Core-ServiceBinderBase-AddMethod-AElf-CSharp-Core-Method-AElf-CSharp-Core-UnaryServerMethod:

AddMethod(method,handler) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Adds a definition for a single request - single response method.

Parameters
''''''''''

+---------+------------------------------------+---------------------+
| Name    | Type                               | Description         |
+=========+====================================+=====================+
| method  | :ref:`AElf.CSharp.Core.Method <AEl\| The method.         |
|         | f-CSharp-Core-Method>`             |                     |
+---------+------------------------------------+---------------------+
| handler | :ref:`AElf.CSharp.Core.UnaryServer\| The method handler. |
|         | Method <AElf-CSharp-Core-UnaryServ\|                     |
|         | erMethod>`                         |                     |
+---------+------------------------------------+---------------------+

Generic Types
'''''''''''''

========= ===========================
Name      Description
========= ===========================
TRequest  The request message class.
TResponse The response message class.
========= ===========================

.. _AElf-CSharp-Core-Extension-TimestampExtensions:

TimestampExtensions ``type``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core.Extension

Summary
'''''''

Helper methods for dealing with protobuf timestamps.

.. _AElf-CSharp-Core-Extension-TimestampExtensions-AddDays-Google-Protobuf-WellKnownTypes-Timestamp-System-Int64:

AddDays(timestamp,days) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Adds a given amount of days to a timestamp. Returns a new instance.

Returns
'''''''

a new timestamp instance.

Parameters
''''''''''

+-----------+----------------------------------+---------------------+
| Name      | Type                             | Description         |
+===========+==================================+=====================+
| timestamp | Google.Protobuf.WellKnown        | the timestamp.      |
|           | Types.Timestamp                  |                     |
+-----------+----------------------------------+---------------------+
| days      | `System.                         | the amount of days. |
|           | Int64 <http://msdn.microsoft.com |                     |
|           | /query/dev14.query?appId=Dev14ID |                     |
|           | EF1&l=EN-US&k=k:System.Int64>`__ |                     |
+-----------+----------------------------------+---------------------+

.. _AElf-CSharp-Core-Extension-TimestampExtensions-AddHours-Google-Protobuf-WellKnownTypes-Timestamp-System-Int64:

AddHours(timestamp,hours) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Adds a given amount of hours to a timestamp. Returns a new instance.

Returns
'''''''

a new timestamp instance.

Parameters
''''''''''

+-----------+---------------------------+----------------------+
| Name      | Type                      | Description          |
+===========+===========================+======================+
| timestamp | Google.Protobuf           | the timestamp.       |
|           | .WellKnownTypes.Timestamp |                      |
+-----------+---------------------------+----------------------+
| hours     | `System.Int64 <http://msd | the amount of hours. |
|           | n.microsoft.com/query/dev |                      |
|           | 14.query?appId=Dev14IDEF1 |                      |
|           | &l=EN-US&k=k:System.Int6  |                      |
|           | 4>`__                     |                      |
+-----------+---------------------------+----------------------+

.. _AElf-CSharp-Core-Extension-TimestampExtensions-AddMilliseconds-Google-Protobuf-WellKnownTypes-Timestamp-System-Int64:

AddMilliseconds(timestamp,milliseconds) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Adds a given amount of milliseconds to a timestamp. Returns a new
instance.

Returns
'''''''

a new timestamp instance.

Parameters
''''''''''

+--------------+--------------------------+--------------------------+
| Name         | Type                     | Description              |
+==============+==========================+==========================+
| timestamp    | Google.Protobuf.         | the timestamp.           |
|              | WellKnownTypes.Timestamp |                          |
+--------------+--------------------------+--------------------------+
| milliseconds | `System.                 | the amount of            |
|              | Int64 <http://msdn.micro | milliseconds to add.     |
|              | soft.com/query/dev14.que |                          |
|              | ry?appId=Dev14IDEF1&l=EN |                          |
|              | -US&k=k:System.Int64>`__ |                          |
+--------------+--------------------------+--------------------------+

.. _AElf-CSharp-Core-Extension-TimestampExtensions-AddMinutes-Google-Protobuf-WellKnownTypes-Timestamp-System-Int64:

AddMinutes(timestamp,minutes) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Adds a given amount of minutes to a timestamp. Returns a new instance.

Returns
'''''''

a new timestamp instance.

Parameters
''''''''''

+-----------+---------------------------+------------------------+
| Name      | Type                      | Description            |
+===========+===========================+========================+
| timestamp | Google.Protobuf           | the timestamp.         |
|           | .WellKnownTypes.Timestamp |                        |
+-----------+---------------------------+------------------------+
| minutes   | `System.Int64 <http://msd | the amount of minutes. |
|           | n.microsoft.com/query/dev |                        |
|           | 14.query?appId=Dev14IDEF1 |                        |
|           | &l=EN-US&k=k:System.Int6  |                        |
|           | 4>`__                     |                        |
+-----------+---------------------------+------------------------+

.. _AElf-CSharp-Core-Extension-TimestampExtensions-AddSeconds-Google-Protobuf-WellKnownTypes-Timestamp-System-Int64:

AddSeconds(timestamp,seconds) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Adds a given amount of seconds to a timestamp. Returns a new instance.

Returns
'''''''

a new timestamp instance.

Parameters
''''''''''

+-----------+---------------------------+------------------------+
| Name      | Type                      | Description            |
+===========+===========================+========================+
| timestamp | Google.Protobuf           | the timestamp.         |
|           | .WellKnownTypes.Timestam  |                        |
+-----------+---------------------------+------------------------+
| seconds   | `System.Int64 <http://msd | the amount of seconds. |
|           | n.microsoft.com/query/dev |                        |
|           | 14.query?appId=Dev14IDEF1 |                        |
|           | &l=EN-US&k=k:System.Int6  |                        |
|           | 4>`__                     |                        |
+-----------+---------------------------+------------------------+


.. _AElf-CSharp-Core-Extension-TimestampExtensions-Max-Google-Protobuf-WellKnownTypes-Timestamp-Google-Protobuf-WellKnownTypes-Timestamp:

Max(timestamp1,timestamp2) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Compares two timestamps and returns the greater one.

Returns
'''''''

the greater timestamp.

Parameters
''''''''''

+------------+---------------------------+----------------------+
| Name       | Type                      | Description          |
+============+===========================+======================+
| timestamp1 | Google.Protobuf           | the first timestamp  |
|            | .WellKnownTypes.Timestamp |                      |
+------------+---------------------------+----------------------+
| timestamp2 | Google.Protobuf           | the second timestamp |
|            | .WellKnownTypes.Timestamp |                      |
+------------+---------------------------+----------------------+

.. _AElf-CSharp-Core-Extension-TimestampExtensions-Milliseconds-Google-Protobuf-WellKnownTypes-Duration:

Milliseconds(duration) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Converts a protobuf duration to long.

Returns
'''''''

the duration represented with a long.

Parameters
''''''''''

+----------+----------------------------+--------------------------+
| Name     | Type                       | Description              |
+==========+============================+==========================+
| duration | Google.Protobuf.           | the duration to convert. |
|          | WellKnownTypes.Duration    |                          |
+----------+----------------------------+--------------------------+

.. _AElf-CSharp-Core-UnaryServerMethod:

UnaryServerMethod ``type``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.CSharp.Core

Summary
'''''''

Handler for a contract method.

Generic Types
'''''''''''''

========= ======================================
Name      Description
========= ======================================
TRequest  Request message type for this method.
TResponse Response message type for this method.
========= ======================================

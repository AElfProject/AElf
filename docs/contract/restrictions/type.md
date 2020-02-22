# Type and Namespace Restrictions

Nodes checks new contract code against below whitelist and if there is a usage of any type that is not covered in the whitelist, or the method access or type name is denied in below whitelist, the deployment will fail.

## Assembly Dependencies

| Assembly | Trust |
| --- | --- |
| netstandard.dll | Partial |
| System.Runtime.dll | Partial |
| System.Runtime.Extensions.dll | Partial |
| System.Private.CoreLib.dll | Partial |
| System.ObjectModel.dll | Partial |
| System.Linq.dll | Full |
| System.Collections | Full |
| Google.Protobuf.dll | Full |
| AElf.Sdk.CSharp.dll | Full |
| AElf.Types.dll | Full |
| AElf.CSharp.Core.dll | Full |
| AElf.Cryptography.dll | Full |

## Types and Members Whitelist in System Namespace

| Type | Member (Field / Method) | Allowed |
| --- | --- | --- |
| `Array` | `AsReadOnly` | Allowed |
| `Func<T>` | ALL | Allowed |
| `Func<T,T>` | ALL | Allowed |
| `Func<T,T,T>` | ALL | Allowed |
| `Nullable<T>` | ALL | Allowed |
| `Environment` | `CurrentManagedThreadId` | Allowed |
| `BitConverter` | `GetBytes` | Allowed |
| `NotImplementedException` | ALL | Allowed |
| `NotSupportedException` | ALL | Allowed |
| `ArgumentOutOfRangeException` | ALL | Allowed |
| `DateTime` | Partially | Allowed |
| `DateTime` | `Now`, `UtcNow`, `Today` | Denied |
| `void` | ALL | Allowed |
| `object` | ALL | Allowed |
| `Type` | ALL | Allowed |
| `IDisposable` | ALL | Allowed |
| `Convert` | ALL | Allowed |
| `Math` | ALL | Allowed |
| `bool` | ALL | Allowed |
| `byte` | ALL | Allowed |
| `sbyte` | ALL | Allowed |
| `char` | ALL | Allowed |
| `int` | ALL | Allowed |
| `uint` | ALL | Allowed |
| `long` | ALL | Allowed |
| `ulong` | ALL | Allowed |
| `decimal` | ALL | Allowed |
| `string` | ALL | Allowed |
| `string` | `Constructor` | Denied |
| `Byte[]` | ALL | Allowed |

## Types and Members Whitelist in System.Reflection Namespace

| Type | Member (Field / Method) | Allowed |
| --- | --- | --- |
| `AssemblyCompanyAttribute` | ALL | Allowed |
| `AssemblyConfigurationAttribute` | ALL | Allowed |
| `AssemblyFileVersionAttribute` | ALL | Allowed |
| `AssemblyInformationalVersionAttribute` | ALL | Allowed |
| `AssemblyProductAttribute` | ALL | Allowed |
| `AssemblyTitleAttribute` | ALL | Allowed |

## Other Whitelisted Namespaces
| Namespace | Type | Member | Allowed |
| --- | --- | --- | --- |
| `System.Linq` | ALL | ALL | Allowed |
| `System.Collections` | ALL | ALL | Allowed |
| `System.Collections.Generic` | ALL | ALL | Allowed |
| `System.Collections.ObjectModel` | ALL | ALL | Allowed |
| `System.Globalization` | `CultureInfo` | `InvariantCulture` | Allowed |
| `System.Runtime.CompilerServices` | `RuntimeHelpers` | `InitializeArray` | Allowed |
| `System.Text` | `Encoding` | `UTF8`, `GetByteCount` | Allowed |

## Allowed Types for Arrays
| Type | Size Limit | Threshold |
| --- | --- | --- |
| `byte` | By Size | 4 Mb |
| `short` | By Size | 4 Mb |
| `int` | By Size | 4 Mb |
| `long` | By Size | 4 Mb |
| `ushort` | By Size | 4 Mb |
| `uint` | By Size | 4 Mb |
| `ulong` | By Size | 4 Mb |
| `decimal` | By Size | 4 Mb |
| `char` | By Size | 4 Mb |
| `string` | By Size | 128 |
| `Type` | By Size | 5 |
| `Object` | By Size | 5 |
| `FileDescriptor` | By Count | 10 |
| `GeneratedClrTypeInfo` | By Count | 100 |

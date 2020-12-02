Type and Namespace Restrictions
===============================

Nodes checks new contract code against below whitelist and if there is a
usage of any type that is not covered in the whitelist, or the method
access or type name is denied in below whitelist, the deployment will
fail.

Assembly Dependencies
---------------------

============================= =======
Assembly                      Trust
============================= =======
netstandard.dll               Partial
System.Runtime.dll            Partial
System.Runtime.Extensions.dll Partial
System.Private.CoreLib.dll    Partial
System.ObjectModel.dll        Partial
System.Linq.dll               Full
System.Collections            Full
Google.Protobuf.dll           Full
AElf.Sdk.CSharp.dll           Full
AElf.Types.dll                Full
AElf.CSharp.Core.dll          Full
AElf.Cryptography.dll         Full
============================= =======

Types and Members Whitelist in System Namespace
-----------------------------------------------

=============================== ============================== =======
Type                            Member (Field / Method)        Allowed
=============================== ============================== =======
``Array``                       ``AsReadOnly``                 Allowed
``Func<T>``                     ALL                            Allowed
``Func<T,T>``                   ALL                            Allowed
``Func<T,T,T>``                 ALL                            Allowed
``Nullable<T>``                 ALL                            Allowed
``Environment``                 ``CurrentManagedThreadId``     Allowed
``BitConverter``                ``GetBytes``                   Allowed
``NotImplementedException``     ALL                            Allowed
``NotSupportedException``       ALL                            Allowed
``ArgumentOutOfRangeException`` ALL                            Allowed
``DateTime``                    Partially                      Allowed
``DateTime``                    ``Now``, ``UtcNow``, ``Today`` Denied
``Uri``                         ``TryCreate``					 Allowed
``Uri``                         ``Scheme``						 Allowed
``Uri``                         ``UriSchemeHttp`` 			 Allowed
``Uri``                         ``UriSchemeHttps`` 			 Allowed
``void``                        ALL                            Allowed
``object``                      ALL                            Allowed
``Type``                        ALL                            Allowed
``IDisposable``                 ALL                            Allowed
``Convert``                     ALL                            Allowed
``Math``                        ALL                            Allowed
``bool``                        ALL                            Allowed
``byte``                        ALL                            Allowed
``sbyte``                       ALL                            Allowed
``char``                        ALL                            Allowed
``int``                         ALL                            Allowed
``uint``                        ALL                            Allowed
``long``                        ALL                            Allowed
``ulong``                       ALL                            Allowed
``decimal``                     ALL                            Allowed
``string``                      ALL                            Allowed
``string``                      ``Constructor``                Denied
``Byte[]``                      ALL                            Allowed
=============================== ============================== =======

Types and Members Whitelist in System.Reflection Namespace
----------------------------------------------------------

+-------------------------------------------+-------------------------+---------+
| Type                                      | Member (Field / Method) | Allowed |
+===========================================+=========================+=========+
| ``AssemblyCompanyAttribute``              | ALL                     | Allowed |
+-------------------------------------------+-------------------------+---------+
| ``AssemblyConfigurationAttribute``        | ALL                     | Allowed |
+-------------------------------------------+-------------------------+---------+
| ``AssemblyFileVersionAttribute``          | ALL                     | Allowed |
+-------------------------------------------+-------------------------+---------+
| ``AssemblyInformationalVersionAttribute`` | ALL                     | Allowed |
+-------------------------------------------+-------------------------+---------+
| ``AssemblyProductAttribute``              | ALL                     | Allowed |
+-------------------------------------------+-------------------------+---------+
| ``AssemblyTitleAttribute``                | ALL                     | Allowed |
+-------------------------------------------+-------------------------+---------+

Other Whitelisted Namespaces
----------------------------

+------------------------------------+-------------------+----------------------+---------+
| Namespace                          | Type              | Member               | Allowed |
+====================================+===================+======================+=========+
| ``System.Linq``                    | ALL               | ALL                  | Allowed |
+------------------------------------+-------------------+----------------------+---------+
| ``System.Collections``             | ALL               | ALL                  | Allowed |
+------------------------------------+-------------------+----------------------+---------+
| ``System.Collections.Generic``     | ALL               | ALL                  | Allowed |
+------------------------------------+-------------------+----------------------+---------+
| ``System.Collections.ObjectModel`` | ALL               | ALL                  | Allowed |
+------------------------------------+-------------------+----------------------+---------+
| ``System.Globalization``           | ``CultureInfo``   | ``InvariantCulture`` | Allowed |
+------------------------------------+-------------------+----------------------+---------+
| ``System.Runtime.CompilerServices``| ``RuntimeHelpers``| ``InitializeArray``  | Allowed |
+------------------------------------+-------------------+----------------------+---------+
| ``System.Text``                    | ``Encoding``      | ``UTF8,GetByteCount``| Allowed |
+------------------------------------+-------------------+----------------------+---------+

Allowed Types for Arrays
------------------------

======================== ================
Type                     Array Size Limit
======================== ================
``byte``                 40960     
``short``                20480     
``int``                  10240     
``long``                 5120      
``ushort``               20480     
``uint``                 10240     
``ulong``                5120      
``decimal``              2560      
``char``                 20480     
``string``               320       
``Type``                 5         
``Object``               5         
``FileDescriptor``       10        
``GeneratedClrTypeInfo`` 100       
======================== ================

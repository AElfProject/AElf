<a name='assembly'></a>
# Vsxmd

## Contents

- [AssemblyUnit](#T-Vsxmd-Units-AssemblyUnit)
  - [#ctor(element)](#M-Vsxmd-Units-AssemblyUnit-#ctor-System-Xml-Linq-XElement- 'Vsxmd.Units.AssemblyUnit.#ctor(System.Xml.Linq.XElement)')
  - [ToMarkdown()](#M-Vsxmd-Units-AssemblyUnit-ToMarkdown 'Vsxmd.Units.AssemblyUnit.ToMarkdown')
- [BaseUnit](#T-Vsxmd-Units-BaseUnit 'Vsxmd.Units.BaseUnit')
  - [#ctor(element,elementName)](#M-Vsxmd-Units-BaseUnit-#ctor-System-Xml-Linq-XElement,System-String- 'Vsxmd.Units.BaseUnit.#ctor(System.Xml.Linq.XElement,System.String)')
  - [Element](#P-Vsxmd-Units-BaseUnit-Element 'Vsxmd.Units.BaseUnit.Element')
  - [ElementContent](#P-Vsxmd-Units-BaseUnit-ElementContent 'Vsxmd.Units.BaseUnit.ElementContent')
  - [GetAttribute(name)](#M-Vsxmd-Units-BaseUnit-GetAttribute-System-Xml-Linq-XName- 'Vsxmd.Units.BaseUnit.GetAttribute(System.Xml.Linq.XName)')
  - [GetChild(name)](#M-Vsxmd-Units-BaseUnit-GetChild-System-Xml-Linq-XName- 'Vsxmd.Units.BaseUnit.GetChild(System.Xml.Linq.XName)')
  - [GetChildren(name)](#M-Vsxmd-Units-BaseUnit-GetChildren-System-Xml-Linq-XName- 'Vsxmd.Units.BaseUnit.GetChildren(System.Xml.Linq.XName)')
  - [ToMarkdown()](#M-Vsxmd-Units-BaseUnit-ToMarkdown 'Vsxmd.Units.BaseUnit.ToMarkdown')
- [Converter](#T-Vsxmd-Converter 'Vsxmd.Converter')
  - [#ctor(document)](#M-Vsxmd-Converter-#ctor-System-Xml-Linq-XDocument- 'Vsxmd.Converter.#ctor(System.Xml.Linq.XDocument)')
  - [ToMarkdown(document)](#M-Vsxmd-Converter-ToMarkdown-System-Xml-Linq-XDocument- 'Vsxmd.Converter.ToMarkdown(System.Xml.Linq.XDocument)')
  - [ToMarkdown()](#M-Vsxmd-Converter-ToMarkdown 'Vsxmd.Converter.ToMarkdown')
- [ExampleUnit](#T-Vsxmd-Units-ExampleUnit 'Vsxmd.Units.ExampleUnit')
  - [#ctor(element)](#M-Vsxmd-Units-ExampleUnit-#ctor-System-Xml-Linq-XElement- 'Vsxmd.Units.ExampleUnit.#ctor(System.Xml.Linq.XElement)')
  - [ToMarkdown()](#M-Vsxmd-Units-ExampleUnit-ToMarkdown 'Vsxmd.Units.ExampleUnit.ToMarkdown')
  - [ToMarkdown(element)](#M-Vsxmd-Units-ExampleUnit-ToMarkdown-System-Xml-Linq-XElement- 'Vsxmd.Units.ExampleUnit.ToMarkdown(System.Xml.Linq.XElement)')
- [ExceptionUnit](#T-Vsxmd-Units-ExceptionUnit 'Vsxmd.Units.ExceptionUnit')
  - [#ctor(element)](#M-Vsxmd-Units-ExceptionUnit-#ctor-System-Xml-Linq-XElement- 'Vsxmd.Units.ExceptionUnit.#ctor(System.Xml.Linq.XElement)')
  - [ToMarkdown()](#M-Vsxmd-Units-ExceptionUnit-ToMarkdown 'Vsxmd.Units.ExceptionUnit.ToMarkdown')
  - [ToMarkdown(elements)](#M-Vsxmd-Units-ExceptionUnit-ToMarkdown-System-Collections-Generic-IEnumerable{System-Xml-Linq-XElement}- 'Vsxmd.Units.ExceptionUnit.ToMarkdown(System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement})')
- [Extensions](#T-Vsxmd-Units-Extensions 'Vsxmd.Units.Extensions')
  - [AsCode(code)](#M-Vsxmd-Units-Extensions-AsCode-System-String- 'Vsxmd.Units.Extensions.AsCode(System.String)')
  - [Escape(content)](#M-Vsxmd-Units-Extensions-Escape-System-String- 'Vsxmd.Units.Extensions.Escape(System.String)')
  - [Join(value,separator)](#M-Vsxmd-Units-Extensions-Join-System-Collections-Generic-IEnumerable{System-String},System-String- 'Vsxmd.Units.Extensions.Join(System.Collections.Generic.IEnumerable{System.String},System.String)')
  - [NthLast\`\`1(source,index)](#M-Vsxmd-Units-Extensions-NthLast``1-System-Collections-Generic-IEnumerable{``0},System-Int32- 'Vsxmd.Units.Extensions.NthLast``1(System.Collections.Generic.IEnumerable{``0},System.Int32)')
  - [Suffix(value,suffix)](#M-Vsxmd-Units-Extensions-Suffix-System-String,System-String- 'Vsxmd.Units.Extensions.Suffix(System.String,System.String)')
  - [TakeAllButLast\`\`1(source,count)](#M-Vsxmd-Units-Extensions-TakeAllButLast``1-System-Collections-Generic-IEnumerable{``0},System-Int32- 'Vsxmd.Units.Extensions.TakeAllButLast``1(System.Collections.Generic.IEnumerable{``0},System.Int32)')
  - [ToAnchor(href)](#M-Vsxmd-Units-Extensions-ToAnchor-System-String- 'Vsxmd.Units.Extensions.ToAnchor(System.String)')
  - [ToHereLink(href)](#M-Vsxmd-Units-Extensions-ToHereLink-System-String- 'Vsxmd.Units.Extensions.ToHereLink(System.String)')
  - [ToLowerString(memberKind)](#M-Vsxmd-Units-Extensions-ToLowerString-Vsxmd-Units-MemberKind- 'Vsxmd.Units.Extensions.ToLowerString(Vsxmd.Units.MemberKind)')
  - [ToMarkdownText(element)](#M-Vsxmd-Units-Extensions-ToMarkdownText-System-Xml-Linq-XElement- 'Vsxmd.Units.Extensions.ToMarkdownText(System.Xml.Linq.XElement)')
  - [ToReferenceLink(memberName,useShortName)](#M-Vsxmd-Units-Extensions-ToReferenceLink-System-String,System-Boolean- 'Vsxmd.Units.Extensions.ToReferenceLink(System.String,System.Boolean)')
- [IConverter](#T-Vsxmd-IConverter 'Vsxmd.IConverter')
  - [ToMarkdown()](#M-Vsxmd-IConverter-ToMarkdown 'Vsxmd.IConverter.ToMarkdown')
- [IUnit](#T-Vsxmd-Units-IUnit 'Vsxmd.Units.IUnit')
  - [ToMarkdown()](#M-Vsxmd-Units-IUnit-ToMarkdown 'Vsxmd.Units.IUnit.ToMarkdown')
- [MemberKind](#T-Vsxmd-Units-MemberKind 'Vsxmd.Units.MemberKind')
  - [Constants](#F-Vsxmd-Units-MemberKind-Constants 'Vsxmd.Units.MemberKind.Constants')
  - [Constructor](#F-Vsxmd-Units-MemberKind-Constructor 'Vsxmd.Units.MemberKind.Constructor')
  - [Method](#F-Vsxmd-Units-MemberKind-Method 'Vsxmd.Units.MemberKind.Method')
  - [NotSupported](#F-Vsxmd-Units-MemberKind-NotSupported 'Vsxmd.Units.MemberKind.NotSupported')
  - [Property](#F-Vsxmd-Units-MemberKind-Property 'Vsxmd.Units.MemberKind.Property')
  - [Type](#F-Vsxmd-Units-MemberKind-Type 'Vsxmd.Units.MemberKind.Type')
- [MemberName](#T-Vsxmd-Units-MemberName 'Vsxmd.Units.MemberName')
  - [#ctor(name,paramNames)](#M-Vsxmd-Units-MemberName-#ctor-System-String,System-Collections-Generic-IEnumerable{System-String}- 'Vsxmd.Units.MemberName.#ctor(System.String,System.Collections.Generic.IEnumerable{System.String})')
  - [#ctor(name)](#M-Vsxmd-Units-MemberName-#ctor-System-String- 'Vsxmd.Units.MemberName.#ctor(System.String)')
  - [Caption](#P-Vsxmd-Units-MemberName-Caption 'Vsxmd.Units.MemberName.Caption')
  - [Kind](#P-Vsxmd-Units-MemberName-Kind 'Vsxmd.Units.MemberName.Kind')
  - [Link](#P-Vsxmd-Units-MemberName-Link 'Vsxmd.Units.MemberName.Link')
  - [Namespace](#P-Vsxmd-Units-MemberName-Namespace 'Vsxmd.Units.MemberName.Namespace')
  - [TypeName](#P-Vsxmd-Units-MemberName-TypeName 'Vsxmd.Units.MemberName.TypeName')
  - [CompareTo()](#M-Vsxmd-Units-MemberName-CompareTo-Vsxmd-Units-MemberName- 'Vsxmd.Units.MemberName.CompareTo(Vsxmd.Units.MemberName)')
  - [GetParamTypes()](#M-Vsxmd-Units-MemberName-GetParamTypes 'Vsxmd.Units.MemberName.GetParamTypes')
  - [ToReferenceLink(useShortName)](#M-Vsxmd-Units-MemberName-ToReferenceLink-System-Boolean- 'Vsxmd.Units.MemberName.ToReferenceLink(System.Boolean)')
- [MemberUnit](#T-Vsxmd-Units-MemberUnit 'Vsxmd.Units.MemberUnit')
  - [#ctor(element)](#M-Vsxmd-Units-MemberUnit-#ctor-System-Xml-Linq-XElement- 'Vsxmd.Units.MemberUnit.#ctor(System.Xml.Linq.XElement)')
  - [Comparer](#P-Vsxmd-Units-MemberUnit-Comparer 'Vsxmd.Units.MemberUnit.Comparer')
  - [Kind](#P-Vsxmd-Units-MemberUnit-Kind 'Vsxmd.Units.MemberUnit.Kind')
  - [Link](#P-Vsxmd-Units-MemberUnit-Link 'Vsxmd.Units.MemberUnit.Link')
  - [TypeName](#P-Vsxmd-Units-MemberUnit-TypeName 'Vsxmd.Units.MemberUnit.TypeName')
  - [ComplementType(group)](#M-Vsxmd-Units-MemberUnit-ComplementType-System-Collections-Generic-IEnumerable{Vsxmd-Units-MemberUnit}- 'Vsxmd.Units.MemberUnit.ComplementType(System.Collections.Generic.IEnumerable{Vsxmd.Units.MemberUnit})')
  - [ToMarkdown()](#M-Vsxmd-Units-MemberUnit-ToMarkdown 'Vsxmd.Units.MemberUnit.ToMarkdown')
- [MemberUnitComparer](#T-Vsxmd-Units-MemberUnit-MemberUnitComparer 'Vsxmd.Units.MemberUnit.MemberUnitComparer')
  - [Compare()](#M-Vsxmd-Units-MemberUnit-MemberUnitComparer-Compare-Vsxmd-Units-MemberUnit,Vsxmd-Units-MemberUnit- 'Vsxmd.Units.MemberUnit.MemberUnitComparer.Compare(Vsxmd.Units.MemberUnit,Vsxmd.Units.MemberUnit)')
- [ParamUnit](#T-Vsxmd-Units-ParamUnit 'Vsxmd.Units.ParamUnit')
  - [#ctor(element,paramType)](#M-Vsxmd-Units-ParamUnit-#ctor-System-Xml-Linq-XElement,System-String- 'Vsxmd.Units.ParamUnit.#ctor(System.Xml.Linq.XElement,System.String)')
  - [ToMarkdown()](#M-Vsxmd-Units-ParamUnit-ToMarkdown 'Vsxmd.Units.ParamUnit.ToMarkdown')
  - [ToMarkdown(elements,paramTypes,memberKind)](#M-Vsxmd-Units-ParamUnit-ToMarkdown-System-Collections-Generic-IEnumerable{System-Xml-Linq-XElement},System-Collections-Generic-IEnumerable{System-String},Vsxmd-Units-MemberKind- 'Vsxmd.Units.ParamUnit.ToMarkdown(System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement},System.Collections.Generic.IEnumerable{System.String},Vsxmd.Units.MemberKind)')
- [PermissionUnit](#T-Vsxmd-Units-PermissionUnit 'Vsxmd.Units.PermissionUnit')
  - [#ctor(element)](#M-Vsxmd-Units-PermissionUnit-#ctor-System-Xml-Linq-XElement- 'Vsxmd.Units.PermissionUnit.#ctor(System.Xml.Linq.XElement)')
  - [ToMarkdown()](#M-Vsxmd-Units-PermissionUnit-ToMarkdown 'Vsxmd.Units.PermissionUnit.ToMarkdown')
  - [ToMarkdown(elements)](#M-Vsxmd-Units-PermissionUnit-ToMarkdown-System-Collections-Generic-IEnumerable{System-Xml-Linq-XElement}- 'Vsxmd.Units.PermissionUnit.ToMarkdown(System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement})')
- [Program](#T-Vsxmd-Program 'Vsxmd.Program')
  - [Main(args)](#M-Vsxmd-Program-Main-System-String[]- 'Vsxmd.Program.Main(System.String[])')
- [RemarksUnit](#T-Vsxmd-Units-RemarksUnit 'Vsxmd.Units.RemarksUnit')
  - [#ctor(element)](#M-Vsxmd-Units-RemarksUnit-#ctor-System-Xml-Linq-XElement- 'Vsxmd.Units.RemarksUnit.#ctor(System.Xml.Linq.XElement)')
  - [ToMarkdown()](#M-Vsxmd-Units-RemarksUnit-ToMarkdown 'Vsxmd.Units.RemarksUnit.ToMarkdown')
  - [ToMarkdown(element)](#M-Vsxmd-Units-RemarksUnit-ToMarkdown-System-Xml-Linq-XElement- 'Vsxmd.Units.RemarksUnit.ToMarkdown(System.Xml.Linq.XElement)')
- [ReturnsUnit](#T-Vsxmd-Units-ReturnsUnit 'Vsxmd.Units.ReturnsUnit')
  - [#ctor(element)](#M-Vsxmd-Units-ReturnsUnit-#ctor-System-Xml-Linq-XElement- 'Vsxmd.Units.ReturnsUnit.#ctor(System.Xml.Linq.XElement)')
  - [ToMarkdown()](#M-Vsxmd-Units-ReturnsUnit-ToMarkdown 'Vsxmd.Units.ReturnsUnit.ToMarkdown')
  - [ToMarkdown(element)](#M-Vsxmd-Units-ReturnsUnit-ToMarkdown-System-Xml-Linq-XElement- 'Vsxmd.Units.ReturnsUnit.ToMarkdown(System.Xml.Linq.XElement)')
- [SeealsoUnit](#T-Vsxmd-Units-SeealsoUnit 'Vsxmd.Units.SeealsoUnit')
  - [#ctor(element)](#M-Vsxmd-Units-SeealsoUnit-#ctor-System-Xml-Linq-XElement- 'Vsxmd.Units.SeealsoUnit.#ctor(System.Xml.Linq.XElement)')
  - [ToMarkdown()](#M-Vsxmd-Units-SeealsoUnit-ToMarkdown 'Vsxmd.Units.SeealsoUnit.ToMarkdown')
  - [ToMarkdown(elements)](#M-Vsxmd-Units-SeealsoUnit-ToMarkdown-System-Collections-Generic-IEnumerable{System-Xml-Linq-XElement}- 'Vsxmd.Units.SeealsoUnit.ToMarkdown(System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement})')
- [SummaryUnit](#T-Vsxmd-Units-SummaryUnit 'Vsxmd.Units.SummaryUnit')
  - [#ctor(element)](#M-Vsxmd-Units-SummaryUnit-#ctor-System-Xml-Linq-XElement- 'Vsxmd.Units.SummaryUnit.#ctor(System.Xml.Linq.XElement)')
  - [ToMarkdown()](#M-Vsxmd-Units-SummaryUnit-ToMarkdown 'Vsxmd.Units.SummaryUnit.ToMarkdown')
  - [ToMarkdown(element)](#M-Vsxmd-Units-SummaryUnit-ToMarkdown-System-Xml-Linq-XElement- 'Vsxmd.Units.SummaryUnit.ToMarkdown(System.Xml.Linq.XElement)')
- [TableOfContents](#T-Vsxmd-TableOfContents 'Vsxmd.TableOfContents')
  - [#ctor(memberUnits)](#M-Vsxmd-TableOfContents-#ctor-System-Linq-IOrderedEnumerable{Vsxmd-Units-MemberUnit}- 'Vsxmd.TableOfContents.#ctor(System.Linq.IOrderedEnumerable{Vsxmd.Units.MemberUnit})')
  - [Link](#P-Vsxmd-TableOfContents-Link 'Vsxmd.TableOfContents.Link')
  - [ToMarkdown()](#M-Vsxmd-TableOfContents-ToMarkdown 'Vsxmd.TableOfContents.ToMarkdown')
- [Test](#T-Vsxmd-Program-Test 'Vsxmd.Program.Test')
  - [#ctor()](#M-Vsxmd-Program-Test-#ctor 'Vsxmd.Program.Test.#ctor')
  - [TestBacktickInSummary()](#M-Vsxmd-Program-Test-TestBacktickInSummary 'Vsxmd.Program.Test.TestBacktickInSummary')
  - [TestGenericException()](#M-Vsxmd-Program-Test-TestGenericException 'Vsxmd.Program.Test.TestGenericException')
  - [TestGenericParameter\`\`2(expression)](#M-Vsxmd-Program-Test-TestGenericParameter``2-System-Linq-Expressions-Expression{System-Func{``0,``1,System-String}}- 'Vsxmd.Program.Test.TestGenericParameter``2(System.Linq.Expressions.Expression{System.Func{``0,``1,System.String}})')
  - [TestGenericPermission()](#M-Vsxmd-Program-Test-TestGenericPermission 'Vsxmd.Program.Test.TestGenericPermission')
  - [TestGenericReference()](#M-Vsxmd-Program-Test-TestGenericReference 'Vsxmd.Program.Test.TestGenericReference')
  - [TestParamWithoutDescription(p)](#M-Vsxmd-Program-Test-TestParamWithoutDescription-System-String- 'Vsxmd.Program.Test.TestParamWithoutDescription(System.String)')
  - [TestSeeLangword()](#M-Vsxmd-Program-Test-TestSeeLangword 'Vsxmd.Program.Test.TestSeeLangword')
  - [TestSpaceAfterInlineElements\`\`1()](#M-Vsxmd-Program-Test-TestSpaceAfterInlineElements``1-System-Boolean- 'Vsxmd.Program.Test.TestSpaceAfterInlineElements``1(System.Boolean)')
- [TestGenericType\`2](#T-Vsxmd-Program-TestGenericType`2 'Vsxmd.Program.TestGenericType`2')
  - [TestGenericMethod\`\`2()](#M-Vsxmd-Program-TestGenericType`2-TestGenericMethod``2 'Vsxmd.Program.TestGenericType`2.TestGenericMethod``2')
- [TypeparamUnit](#T-Vsxmd-Units-TypeparamUnit 'Vsxmd.Units.TypeparamUnit')
  - [#ctor(element)](#M-Vsxmd-Units-TypeparamUnit-#ctor-System-Xml-Linq-XElement- 'Vsxmd.Units.TypeparamUnit.#ctor(System.Xml.Linq.XElement)')
  - [ToMarkdown()](#M-Vsxmd-Units-TypeparamUnit-ToMarkdown 'Vsxmd.Units.TypeparamUnit.ToMarkdown')
  - [ToMarkdown(elements)](#M-Vsxmd-Units-TypeparamUnit-ToMarkdown-System-Collections-Generic-IEnumerable{System-Xml-Linq-XElement}- 'Vsxmd.Units.TypeparamUnit.ToMarkdown(System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement})')

<a name='T-Vsxmd-Units-AssemblyUnit'></a>
## AssemblyUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Assembly unit.

<a name='M-Vsxmd-Units-AssemblyUnit-#ctor-System-Xml-Linq-XElement-'></a>
### #ctor(element) `constructor`

##### Summary

Initializes a new instance of the [AssemblyUnit](#T-Vsxmd-Units-AssemblyUnit 'Vsxmd.Units.AssemblyUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The assembly XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `assembly`. |

<a name='M-Vsxmd-Units-AssemblyUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='T-Vsxmd-Units-BaseUnit'></a>
## BaseUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

The base unit.

<a name='M-Vsxmd-Units-BaseUnit-#ctor-System-Xml-Linq-XElement,System-String-'></a>
### #ctor(element,elementName) `constructor`

##### Summary

Initializes a new instance of the [BaseUnit](#T-Vsxmd-Units-BaseUnit 'Vsxmd.Units.BaseUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The XML element. |
| elementName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The expected XML element name. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML `element` name not matches the expected `elementName`. |

<a name='P-Vsxmd-Units-BaseUnit-Element'></a>
### Element `property`

##### Summary

Gets the XML element.

<a name='P-Vsxmd-Units-BaseUnit-ElementContent'></a>
### ElementContent `property`

##### Summary

Gets the Markdown content representing the element.

<a name='M-Vsxmd-Units-BaseUnit-GetAttribute-System-Xml-Linq-XName-'></a>
### GetAttribute(name) `method`

##### Summary

Returns the [XAttribute](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XAttribute 'System.Xml.Linq.XAttribute') value of this [XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') that has the specified `name`.

##### Returns

An [XAttribute](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XAttribute 'System.Xml.Linq.XAttribute') value that has the specified `name`; `null` if there is no attribute with the specified `name`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.Xml.Linq.XName](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XName 'System.Xml.Linq.XName') | The [XName](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XName 'System.Xml.Linq.XName') of the [XAttribute](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XAttribute 'System.Xml.Linq.XAttribute') to get. |

<a name='M-Vsxmd-Units-BaseUnit-GetChild-System-Xml-Linq-XName-'></a>
### GetChild(name) `method`

##### Summary

Gets the first (in document order) child element with the specified `name`.

##### Returns

A [XName](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XName 'System.Xml.Linq.XName') that matches the specified `name`, or `null`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.Xml.Linq.XName](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XName 'System.Xml.Linq.XName') | The [XName](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XName 'System.Xml.Linq.XName') to match. |

<a name='M-Vsxmd-Units-BaseUnit-GetChildren-System-Xml-Linq-XName-'></a>
### GetChildren(name) `method`

##### Summary

Returns a collection of the child elements of this element or document, in document order.
Only elements that have a matching [XName](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XName 'System.Xml.Linq.XName') are included in the collection.

##### Returns

An [IEnumerable\`1](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable`1 'System.Collections.Generic.IEnumerable`1') of [XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') containing the children that have a matching [XName](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XName 'System.Xml.Linq.XName'), in document order.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.Xml.Linq.XName](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XName 'System.Xml.Linq.XName') | The [XName](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XName 'System.Xml.Linq.XName') to match. |

<a name='M-Vsxmd-Units-BaseUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='T-Vsxmd-Converter'></a>
## Converter `type`

##### Namespace

Vsxmd

##### Summary

*Inherit from parent.*

<a name='M-Vsxmd-Converter-#ctor-System-Xml-Linq-XDocument-'></a>
### #ctor(document) `constructor`

##### Summary

Initializes a new instance of the [Converter](#T-Vsxmd-Converter 'Vsxmd.Converter') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| document | [System.Xml.Linq.XDocument](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XDocument 'System.Xml.Linq.XDocument') | The XML document. |

<a name='M-Vsxmd-Converter-ToMarkdown-System-Xml-Linq-XDocument-'></a>
### ToMarkdown(document) `method`

##### Summary

Convert VS XML document to Markdown syntax.

##### Returns

The generated Markdown content.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| document | [System.Xml.Linq.XDocument](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XDocument 'System.Xml.Linq.XDocument') | The XML document. |

<a name='M-Vsxmd-Converter-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='T-Vsxmd-Units-ExampleUnit'></a>
## ExampleUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Example unit.

<a name='M-Vsxmd-Units-ExampleUnit-#ctor-System-Xml-Linq-XElement-'></a>
### #ctor(element) `constructor`

##### Summary

Initializes a new instance of the [ExampleUnit](#T-Vsxmd-Units-ExampleUnit 'Vsxmd.Units.ExampleUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The example XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `example`. |

<a name='M-Vsxmd-Units-ExampleUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Units-ExampleUnit-ToMarkdown-System-Xml-Linq-XElement-'></a>
### ToMarkdown(element) `method`

##### Summary

Convert the example XML element to Markdown safely.
If element is `null`, return empty string.

##### Returns

The generated Markdown.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The example XML element. |

<a name='T-Vsxmd-Units-ExceptionUnit'></a>
## ExceptionUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Exception unit.

<a name='M-Vsxmd-Units-ExceptionUnit-#ctor-System-Xml-Linq-XElement-'></a>
### #ctor(element) `constructor`

##### Summary

Initializes a new instance of the [ExceptionUnit](#T-Vsxmd-Units-ExceptionUnit 'Vsxmd.Units.ExceptionUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The exception XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `exception`. |

<a name='M-Vsxmd-Units-ExceptionUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Units-ExceptionUnit-ToMarkdown-System-Collections-Generic-IEnumerable{System-Xml-Linq-XElement}-'></a>
### ToMarkdown(elements) `method`

##### Summary

Convert the exception XML element to Markdown safely.
If element is `null`, return empty string.

##### Returns

The generated Markdown.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| elements | [System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement}') | The exception XML element list. |

<a name='T-Vsxmd-Units-Extensions'></a>
## Extensions `type`

##### Namespace

Vsxmd.Units

##### Summary

Extensions helper.

<a name='M-Vsxmd-Units-Extensions-AsCode-System-String-'></a>
### AsCode(code) `method`

##### Summary

Wrap the `code` into Markdown backtick safely.

The backtick characters inside the `code` reverse as it is.

##### Returns

The Markdown code span.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| code | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The code span. |

##### Remarks

Reference: http://meta.stackexchange.com/questions/55437/how-can-the-backtick-character-be-included-in-code .

<a name='M-Vsxmd-Units-Extensions-Escape-System-String-'></a>
### Escape(content) `method`

##### Summary

Escape the content to keep it raw in Markdown syntax.

##### Returns

The escaped content.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| content | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The content. |

<a name='M-Vsxmd-Units-Extensions-Join-System-Collections-Generic-IEnumerable{System-String},System-String-'></a>
### Join(value,separator) `method`

##### Summary

Concatenates the `value`s with the `separator`.

##### Returns

The concatenated string.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.Collections.Generic.IEnumerable{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.String}') | The string values. |
| separator | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The separator. |

<a name='M-Vsxmd-Units-Extensions-NthLast``1-System-Collections-Generic-IEnumerable{``0},System-Int32-'></a>
### NthLast\`\`1(source,index) `method`

##### Summary

Gets the n-th last element from the `source`.

##### Returns

The element at the specified position in the `source` sequence.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| source | [System.Collections.Generic.IEnumerable{\`\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{``0}') | The source enumerable. |
| index | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The index for the n-th last. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TSource | The type of the elements of `source`. |

<a name='M-Vsxmd-Units-Extensions-Suffix-System-String,System-String-'></a>
### Suffix(value,suffix) `method`

##### Summary

Suffix the `suffix` to the `value`, and generate a new string.

##### Returns

The new string.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The original string value. |
| suffix | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The suffix string. |

<a name='M-Vsxmd-Units-Extensions-TakeAllButLast``1-System-Collections-Generic-IEnumerable{``0},System-Int32-'></a>
### TakeAllButLast\`\`1(source,count) `method`

##### Summary

Take all element except the last `count`.

##### Returns

The generated enumerable.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| source | [System.Collections.Generic.IEnumerable{\`\`0}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{``0}') | The source enumerable. |
| count | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The number to except. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TSource | The type of the elements of `source`. |

<a name='M-Vsxmd-Units-Extensions-ToAnchor-System-String-'></a>
### ToAnchor(href) `method`

##### Summary

Generate an anchor for the `href`.

##### Returns

The anchor for the `href`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| href | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The href. |

<a name='M-Vsxmd-Units-Extensions-ToHereLink-System-String-'></a>
### ToHereLink(href) `method`

##### Summary

Generate "to here" link for the `href`.

##### Returns

The "to here" link for the `href`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| href | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The href. |

<a name='M-Vsxmd-Units-Extensions-ToLowerString-Vsxmd-Units-MemberKind-'></a>
### ToLowerString(memberKind) `method`

##### Summary

Convert the [MemberKind](#T-Vsxmd-Units-MemberKind 'Vsxmd.Units.MemberKind') to its lowercase name.

##### Returns

The member kind's lowercase name.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| memberKind | [Vsxmd.Units.MemberKind](#T-Vsxmd-Units-MemberKind 'Vsxmd.Units.MemberKind') | The member kind. |

<a name='M-Vsxmd-Units-Extensions-ToMarkdownText-System-Xml-Linq-XElement-'></a>
### ToMarkdownText(element) `method`

##### Summary

Convert the inline XML nodes to Markdown text.
For example, it works for `summary` and `returns` elements.

##### Returns

The generated Markdown content.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The XML element. |

##### Example

This method converts the following `summary` element.

```
<summary>The <paramref name="element" /> value is <value>null</value>, it throws <c>ArgumentException</c>. For more, see <see cref="M:Vsxmd.Units.Extensions.ToMarkdownText(System.Xml.Linq.XElement)" />.</summary>
```

To the below Markdown content.

```
The `element` value is `null`, it throws `ArgumentException`. For more, see `ToMarkdownText`.
```

<a name='M-Vsxmd-Units-Extensions-ToReferenceLink-System-String,System-Boolean-'></a>
### ToReferenceLink(memberName,useShortName) `method`

##### Summary

Generate the reference link for the `memberName`.

##### Returns

The generated reference link.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| memberName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The member name. |
| useShortName | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Indicate if use short type name. |

##### Example

For `T:Vsxmd.Units.MemberUnit`, convert it to `[MemberUnit](#T-Vsxmd.Units.MemberUnit)`.

For `T:System.ArgumentException`, convert it to `[ArgumentException](http://msdn/path/to/System.ArgumentException)`.

<a name='T-Vsxmd-IConverter'></a>
## IConverter `type`

##### Namespace

Vsxmd

##### Summary

Converter for XML document to Markdown syntax conversion.

<a name='M-Vsxmd-IConverter-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

Convert to Markdown syntax.

##### Returns

The generated Markdown content.

##### Parameters

This method has no parameters.

<a name='T-Vsxmd-Units-IUnit'></a>
## IUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

[IUnit](#T-Vsxmd-Units-IUnit 'Vsxmd.Units.IUnit') is wrapper to handle XML elements.

<a name='M-Vsxmd-Units-IUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

Represent the XML element content as Markdown syntax.

##### Returns

The generated Markdown content.

##### Parameters

This method has no parameters.

<a name='T-Vsxmd-Units-MemberKind'></a>
## MemberKind `type`

##### Namespace

Vsxmd.Units

##### Summary

The member kind.

<a name='F-Vsxmd-Units-MemberKind-Constants'></a>
### Constants `constants`

##### Summary

Constants

<a name='F-Vsxmd-Units-MemberKind-Constructor'></a>
### Constructor `constants`

##### Summary

Constructor.

<a name='F-Vsxmd-Units-MemberKind-Method'></a>
### Method `constants`

##### Summary

Method.

<a name='F-Vsxmd-Units-MemberKind-NotSupported'></a>
### NotSupported `constants`

##### Summary

Not supported member kind.

<a name='F-Vsxmd-Units-MemberKind-Property'></a>
### Property `constants`

##### Summary

Property.

<a name='F-Vsxmd-Units-MemberKind-Type'></a>
### Type `constants`

##### Summary

Type.

<a name='T-Vsxmd-Units-MemberName'></a>
## MemberName `type`

##### Namespace

Vsxmd.Units

##### Summary

Member name.

<a name='M-Vsxmd-Units-MemberName-#ctor-System-String,System-Collections-Generic-IEnumerable{System-String}-'></a>
### #ctor(name,paramNames) `constructor`

##### Summary

Initializes a new instance of the [MemberName](#T-Vsxmd-Units-MemberName 'Vsxmd.Units.MemberName') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The raw member name. For example, `T:Vsxmd.Units.MemberName`. |
| paramNames | [System.Collections.Generic.IEnumerable{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.String}') | The parameter names. It is only used when member kind is [Constructor](#F-Vsxmd-Units-MemberKind-Constructor 'Vsxmd.Units.MemberKind.Constructor') or [Method](#F-Vsxmd-Units-MemberKind-Method 'Vsxmd.Units.MemberKind.Method'). |

<a name='M-Vsxmd-Units-MemberName-#ctor-System-String-'></a>
### #ctor(name) `constructor`

##### Summary

Initializes a new instance of the [MemberName](#T-Vsxmd-Units-MemberName 'Vsxmd.Units.MemberName') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The raw member name. For example, `T:Vsxmd.Units.MemberName`. |

<a name='P-Vsxmd-Units-MemberName-Caption'></a>
### Caption `property`

##### Summary

Gets the caption representation for this member name.

##### Example

For [Type](#F-Vsxmd-Units-MemberKind-Type 'Vsxmd.Units.MemberKind.Type'), show as `## Vsxmd.Units.MemberName [#](#here) [^](#contents)`.

For other kinds, show as `### Vsxmd.Units.MemberName.Caption [#](#here) [^](#contents)`.

<a name='P-Vsxmd-Units-MemberName-Kind'></a>
### Kind `property`

##### Summary

Gets the member kind, one of [MemberKind](#T-Vsxmd-Units-MemberKind 'Vsxmd.Units.MemberKind').

<a name='P-Vsxmd-Units-MemberName-Link'></a>
### Link `property`

##### Summary

Gets the link pointing to this member unit.

<a name='P-Vsxmd-Units-MemberName-Namespace'></a>
### Namespace `property`

##### Summary

Gets the namespace name.

##### Example

`System`, `Vsxmd`, `Vsxmd.Units`.

<a name='P-Vsxmd-Units-MemberName-TypeName'></a>
### TypeName `property`

##### Summary

Gets the type name.

##### Example

`Vsxmd.Program`, `Vsxmd.Units.TypeUnit`.

<a name='M-Vsxmd-Units-MemberName-CompareTo-Vsxmd-Units-MemberName-'></a>
### CompareTo() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Units-MemberName-GetParamTypes'></a>
### GetParamTypes() `method`

##### Summary

Gets the method parameter type names from the member name.

##### Returns

The method parameter type name list.

##### Parameters

This method has no parameters.

##### Example

It will prepend the type kind character (`T:`) to the type string.

For `(System.String,System.Int32)`, returns `["T:System.String","T:System.Int32"]`.

It also handle generic type.

For `(System.Collections.Generic.IEnumerable{System.String})`, returns `["T:System.Collections.Generic.IEnumerable{System.String}"]`.

<a name='M-Vsxmd-Units-MemberName-ToReferenceLink-System-Boolean-'></a>
### ToReferenceLink(useShortName) `method`

##### Summary

Convert the member name to Markdown reference link.

If then name is under `System` namespace, the link points to MSDN.

Otherwise, the link points to this page anchor.

##### Returns

The generated Markdown reference link.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| useShortName | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Indicate if use short type name. |

<a name='T-Vsxmd-Units-MemberUnit'></a>
## MemberUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Member unit.

<a name='M-Vsxmd-Units-MemberUnit-#ctor-System-Xml-Linq-XElement-'></a>
### #ctor(element) `constructor`

##### Summary

Initializes a new instance of the [MemberUnit](#T-Vsxmd-Units-MemberUnit 'Vsxmd.Units.MemberUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The member XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `member`. |

<a name='P-Vsxmd-Units-MemberUnit-Comparer'></a>
### Comparer `property`

##### Summary

Gets the member unit comparer.

<a name='P-Vsxmd-Units-MemberUnit-Kind'></a>
### Kind `property`

##### Summary

Gets the member kind, one of [MemberKind](#T-Vsxmd-Units-MemberKind 'Vsxmd.Units.MemberKind').

<a name='P-Vsxmd-Units-MemberUnit-Link'></a>
### Link `property`

##### Summary

Gets the link pointing to this member unit.

<a name='P-Vsxmd-Units-MemberUnit-TypeName'></a>
### TypeName `property`

##### Summary

Gets the type name.

##### Example

`Vsxmd.Program`, `Vsxmd.Units.TypeUnit`.

<a name='M-Vsxmd-Units-MemberUnit-ComplementType-System-Collections-Generic-IEnumerable{Vsxmd-Units-MemberUnit}-'></a>
### ComplementType(group) `method`

##### Summary

Complement a type unit if the member unit `group` does not have one.
One member unit group has the same [TypeName](#P-Vsxmd-Units-MemberUnit-TypeName 'Vsxmd.Units.MemberUnit.TypeName').

##### Returns

The complemented member unit group.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| group | [System.Collections.Generic.IEnumerable{Vsxmd.Units.MemberUnit}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{Vsxmd.Units.MemberUnit}') | The member unit group. |

<a name='M-Vsxmd-Units-MemberUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='T-Vsxmd-Units-MemberUnit-MemberUnitComparer'></a>
## MemberUnitComparer `type`

##### Namespace

Vsxmd.Units.MemberUnit

<a name='M-Vsxmd-Units-MemberUnit-MemberUnitComparer-Compare-Vsxmd-Units-MemberUnit,Vsxmd-Units-MemberUnit-'></a>
### Compare() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='T-Vsxmd-Units-ParamUnit'></a>
## ParamUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Param unit.

<a name='M-Vsxmd-Units-ParamUnit-#ctor-System-Xml-Linq-XElement,System-String-'></a>
### #ctor(element,paramType) `constructor`

##### Summary

Initializes a new instance of the [ParamUnit](#T-Vsxmd-Units-ParamUnit 'Vsxmd.Units.ParamUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The param XML element. |
| paramType | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The parameter type corresponding to the param XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `param`. |

<a name='M-Vsxmd-Units-ParamUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Units-ParamUnit-ToMarkdown-System-Collections-Generic-IEnumerable{System-Xml-Linq-XElement},System-Collections-Generic-IEnumerable{System-String},Vsxmd-Units-MemberKind-'></a>
### ToMarkdown(elements,paramTypes,memberKind) `method`

##### Summary

Convert the param XML element to Markdown safely.

##### Returns

The generated Markdown.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| elements | [System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement}') | The param XML element list. |
| paramTypes | [System.Collections.Generic.IEnumerable{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.String}') | The paramater type names. |
| memberKind | [Vsxmd.Units.MemberKind](#T-Vsxmd-Units-MemberKind 'Vsxmd.Units.MemberKind') | The member kind of the parent element. |

##### Remarks

When the parameter (a.k.a `elements`) list is empty:

If parent element kind is [Constructor](#F-Vsxmd-Units-MemberKind-Constructor 'Vsxmd.Units.MemberKind.Constructor') or [Method](#F-Vsxmd-Units-MemberKind-Method 'Vsxmd.Units.MemberKind.Method'), it returns a hint about "no parameters".

If parent element kind is not the value mentioned above, it returns an empty string.

<a name='T-Vsxmd-Units-PermissionUnit'></a>
## PermissionUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Permission unit.

<a name='M-Vsxmd-Units-PermissionUnit-#ctor-System-Xml-Linq-XElement-'></a>
### #ctor(element) `constructor`

##### Summary

Initializes a new instance of the [PermissionUnit](#T-Vsxmd-Units-PermissionUnit 'Vsxmd.Units.PermissionUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The permission XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `permission`. |

<a name='M-Vsxmd-Units-PermissionUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Units-PermissionUnit-ToMarkdown-System-Collections-Generic-IEnumerable{System-Xml-Linq-XElement}-'></a>
### ToMarkdown(elements) `method`

##### Summary

Convert the permission XML element to Markdown safely.
If element is `null`, return empty string.

##### Returns

The generated Markdown.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| elements | [System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement}') | The permission XML element list. |

<a name='T-Vsxmd-Program'></a>
## Program `type`

##### Namespace

Vsxmd

##### Summary

Program entry.

##### Remarks

Usage syntax:

```
Vsxmd.exe &lt;input-XML-path&gt; [output-Markdown-path]
```

The `input-XML-path` argument is required. It references to the VS generated XML documentation file.

The `output-Markdown-path` argument is optional. It indicates the file path for the Markdown output file. When not specific, it will be a `.md` file with same file name as the XML documentation file, path at the XML documentation folder.

<a name='M-Vsxmd-Program-Main-System-String[]-'></a>
### Main(args) `method`

##### Summary

Program main function entry.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| args | [System.String[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String[] 'System.String[]') | Program arguments. |

##### See Also

- [Vsxmd.Program](#T-Vsxmd-Program 'Vsxmd.Program')

<a name='T-Vsxmd-Units-RemarksUnit'></a>
## RemarksUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Remarks unit.

<a name='M-Vsxmd-Units-RemarksUnit-#ctor-System-Xml-Linq-XElement-'></a>
### #ctor(element) `constructor`

##### Summary

Initializes a new instance of the [RemarksUnit](#T-Vsxmd-Units-RemarksUnit 'Vsxmd.Units.RemarksUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The remarks XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `remarks`. |

<a name='M-Vsxmd-Units-RemarksUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Units-RemarksUnit-ToMarkdown-System-Xml-Linq-XElement-'></a>
### ToMarkdown(element) `method`

##### Summary

Convert the remarks XML element to Markdown safely.
If element is `null`, return empty string.

##### Returns

The generated Markdown.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The remarks XML element. |

<a name='T-Vsxmd-Units-ReturnsUnit'></a>
## ReturnsUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Returns unit.

<a name='M-Vsxmd-Units-ReturnsUnit-#ctor-System-Xml-Linq-XElement-'></a>
### #ctor(element) `constructor`

##### Summary

Initializes a new instance of the [ReturnsUnit](#T-Vsxmd-Units-ReturnsUnit 'Vsxmd.Units.ReturnsUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The returns XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `returns`. |

<a name='M-Vsxmd-Units-ReturnsUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Units-ReturnsUnit-ToMarkdown-System-Xml-Linq-XElement-'></a>
### ToMarkdown(element) `method`

##### Summary

Convert the returns XML element to Markdown safely.
If element is `null`, return empty string.

##### Returns

The generated Markdown.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The returns XML element. |

<a name='T-Vsxmd-Units-SeealsoUnit'></a>
## SeealsoUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Seealso unit.

<a name='M-Vsxmd-Units-SeealsoUnit-#ctor-System-Xml-Linq-XElement-'></a>
### #ctor(element) `constructor`

##### Summary

Initializes a new instance of the [SeealsoUnit](#T-Vsxmd-Units-SeealsoUnit 'Vsxmd.Units.SeealsoUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The seealso XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `seealso`. |

<a name='M-Vsxmd-Units-SeealsoUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Units-SeealsoUnit-ToMarkdown-System-Collections-Generic-IEnumerable{System-Xml-Linq-XElement}-'></a>
### ToMarkdown(elements) `method`

##### Summary

Convert the seealso XML element to Markdown safely.
If element is `null`, return empty string.

##### Returns

The generated Markdown.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| elements | [System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement}') | The seealso XML element list. |

<a name='T-Vsxmd-Units-SummaryUnit'></a>
## SummaryUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Summary unit.

<a name='M-Vsxmd-Units-SummaryUnit-#ctor-System-Xml-Linq-XElement-'></a>
### #ctor(element) `constructor`

##### Summary

Initializes a new instance of the [SummaryUnit](#T-Vsxmd-Units-SummaryUnit 'Vsxmd.Units.SummaryUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The summary XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `summary`. |

<a name='M-Vsxmd-Units-SummaryUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Units-SummaryUnit-ToMarkdown-System-Xml-Linq-XElement-'></a>
### ToMarkdown(element) `method`

##### Summary

Convert the summary XML element to Markdown safely.
If element is `null`, return empty string.

##### Returns

The generated Markdown.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The summary XML element. |

<a name='T-Vsxmd-TableOfContents'></a>
## TableOfContents `type`

##### Namespace

Vsxmd

##### Summary

Table of contents.

<a name='M-Vsxmd-TableOfContents-#ctor-System-Linq-IOrderedEnumerable{Vsxmd-Units-MemberUnit}-'></a>
### #ctor(memberUnits) `constructor`

##### Summary

Initializes a new instance of the [TableOfContents](#T-Vsxmd-TableOfContents 'Vsxmd.TableOfContents') class.

It convert the table of contents from the `memberUnits`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| memberUnits | [System.Linq.IOrderedEnumerable{Vsxmd.Units.MemberUnit}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Linq.IOrderedEnumerable 'System.Linq.IOrderedEnumerable{Vsxmd.Units.MemberUnit}') | The member unit list. |

<a name='P-Vsxmd-TableOfContents-Link'></a>
### Link `property`

##### Summary

Gets the link pointing to the table of contents.

<a name='M-Vsxmd-TableOfContents-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

Convert the table of contents to Markdown syntax.

##### Returns

The table of contents in Markdown syntax.

##### Parameters

This method has no parameters.

<a name='T-Vsxmd-Program-Test'></a>
## Test `type`

##### Namespace

Vsxmd.Program

<a name='M-Vsxmd-Program-Test-#ctor'></a>
### #ctor() `constructor`

##### Summary

Initializes a new instance of the [Test](#T-Vsxmd-Program-Test 'Vsxmd.Program.Test') class.

Test constructor without parameters.

See [Test.#ctor](#M-Vsxmd-Program-Test-#ctor 'Vsxmd.Program.Test.#ctor').

##### Parameters

This constructor has no parameters.

##### Permissions

| Name | Description |
| ---- | ----------- |
| [Vsxmd.Program](#T-Vsxmd-Program 'Vsxmd.Program') | Just for test. |

<a name='M-Vsxmd-Program-Test-TestBacktickInSummary'></a>
### TestBacktickInSummary() `method`

##### Summary

Test backtick characters in summary comment.

See \`should not inside code block\`.

See `` `backtick inside code block` ``.

See \``code block inside backtick`\`.

##### Returns

Nothing.

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Program-Test-TestGenericException'></a>
### TestGenericException() `method`

##### Summary

Test generic exception type.

##### Returns

Nothing.

##### Parameters

This method has no parameters.

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [Vsxmd.Program.Test.TestGenericParameter\`\`2](#M-Vsxmd-Program-Test-TestGenericParameter``2-System-Linq-Expressions-Expression{System-Func{``0,``1,System-String}}- 'Vsxmd.Program.Test.TestGenericParameter``2(System.Linq.Expressions.Expression{System.Func{``0,``1,System.String}})') | Just for test. |

<a name='M-Vsxmd-Program-Test-TestGenericParameter``2-System-Linq-Expressions-Expression{System-Func{``0,``1,System-String}}-'></a>
### TestGenericParameter\`\`2(expression) `method`

##### Summary

Test generic parameter type.

See `T1` and `T2`.

##### Returns

Nothing.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| expression | [System.Linq.Expressions.Expression{System.Func{\`\`0,\`\`1,System.String}}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Linq.Expressions.Expression 'System.Linq.Expressions.Expression{System.Func{``0,``1,System.String}}') | The linq expression. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T1 | Generic type 1. |
| T2 | Generic type 2. |

<a name='M-Vsxmd-Program-Test-TestGenericPermission'></a>
### TestGenericPermission() `method`

##### Summary

Test generic exception type.

##### Returns

Nothing.

##### Parameters

This method has no parameters.

##### Permissions

| Name | Description |
| ---- | ----------- |
| [Vsxmd.Program.Test.TestGenericParameter\`\`2](#M-Vsxmd-Program-Test-TestGenericParameter``2-System-Linq-Expressions-Expression{System-Func{``0,``1,System-String}}- 'Vsxmd.Program.Test.TestGenericParameter``2(System.Linq.Expressions.Expression{System.Func{``0,``1,System.String}})') | Just for test. |

<a name='M-Vsxmd-Program-Test-TestGenericReference'></a>
### TestGenericReference() `method`

##### Summary

Test generic reference type.

See [TestGenericParameter\`\`2](#M-Vsxmd-Program-Test-TestGenericParameter``2-System-Linq-Expressions-Expression{System-Func{``0,``1,System-String}}- 'Vsxmd.Program.Test.TestGenericParameter``2(System.Linq.Expressions.Expression{System.Func{``0,``1,System.String}})').

##### Returns

Nothing.

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Program-Test-TestParamWithoutDescription-System-String-'></a>
### TestParamWithoutDescription(p) `method`

##### Summary

Test a param tag without description.

##### Returns

Nothing.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| p | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

<a name='M-Vsxmd-Program-Test-TestSeeLangword'></a>
### TestSeeLangword() `method`

##### Summary

Test see tag with langword attribute. See `true`.

##### Returns

Nothing.

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Program-Test-TestSpaceAfterInlineElements``1-System-Boolean-'></a>
### TestSpaceAfterInlineElements\`\`1() `method`

##### Summary

Test space after inline elements.

See `code block` should follow a space.

See a value at the end of a `sentence`.

See [TestSpaceAfterInlineElements\`\`1](#M-Vsxmd-Program-Test-TestSpaceAfterInlineElements``1-System-Boolean- 'Vsxmd.Program.Test.TestSpaceAfterInlineElements``1(System.Boolean)') as a link.

See `space` after a param ref.

See `T` after a type param ref.

##### Returns

Nothing.

##### Parameters

This method has no parameters.

<a name='T-Vsxmd-Program-TestGenericType`2'></a>
## TestGenericType\`2 `type`

##### Namespace

Vsxmd.Program

##### Summary

Test generic type.

See [TestGenericType\`2](#T-Vsxmd-Program-TestGenericType`2 'Vsxmd.Program.TestGenericType`2').

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T1 | Generic type 1. |
| T2 | Generic type 2. |

<a name='M-Vsxmd-Program-TestGenericType`2-TestGenericMethod``2'></a>
### TestGenericMethod\`\`2() `method`

##### Summary

Test generic method.

See [TestGenericMethod\`\`2](#M-Vsxmd-Program-TestGenericType`2-TestGenericMethod``2 'Vsxmd.Program.TestGenericType`2.TestGenericMethod``2').

##### Returns

Nothing.

##### Parameters

This method has no parameters.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T3 | Generic type 3. |
| T4 | Generic type 4. |

<a name='T-Vsxmd-Units-TypeparamUnit'></a>
## TypeparamUnit `type`

##### Namespace

Vsxmd.Units

##### Summary

Typeparam unit.

<a name='M-Vsxmd-Units-TypeparamUnit-#ctor-System-Xml-Linq-XElement-'></a>
### #ctor(element) `constructor`

##### Summary

Initializes a new instance of the [TypeparamUnit](#T-Vsxmd-Units-TypeparamUnit 'Vsxmd.Units.TypeparamUnit') class.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| element | [System.Xml.Linq.XElement](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Xml.Linq.XElement 'System.Xml.Linq.XElement') | The typeparam XML element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') | Throw if XML element name is not `typeparam`. |

<a name='M-Vsxmd-Units-TypeparamUnit-ToMarkdown'></a>
### ToMarkdown() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Vsxmd-Units-TypeparamUnit-ToMarkdown-System-Collections-Generic-IEnumerable{System-Xml-Linq-XElement}-'></a>
### ToMarkdown(elements) `method`

##### Summary

Convert the param XML element to Markdown safely.
If element is `null`, return empty string.

##### Returns

The generated Markdown.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| elements | [System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.Xml.Linq.XElement}') | The param XML element list. |
# API Catalog Schema

The format of the catalog is inspired by the .NET metadata format (ECMA 335). It
is comprised of heaps and tables.

The data is separated into two sections, the header and the body:

* `Header`
* Deflate compressed body

In order to read the format the header will be read first and then then body
will need to be decompressed.

## Header

| Offset | Length | Type        | Name        |
| ------ | ------ | ----------- | ----------- |
| 0      | 8      | `Char8[8]`  | Magic Value |
| 8      | 4      | `Int32`     | Version     |
| 12     | 52     | `Int32[13]` | Table Sizes |

- The magic value is `APICATFB`
- This is version `10`
- The table sizes are in the following order:
  1. [String Heap]
  1. [Blob Heap]
  1. [Platform Table]
  1. [Framework Table]
  1. [Package Table]
  1. [Assembly Table]
  1. [API Table]
  1. [Root API Table]
  1. [Extension Methods Table]
  1. [Obsoletion Table]
  1. [Platform Support Table]
  1. [Preview Requirement Table]
  1. [Experimental Table]

## String Heap

Stores length-prefixed UTF8 encoded strings. Users will have an offset that
points into the heap.

| Type       | Length | Comment             |
| ---------- | ------ | ------------------- |
| `Int32`    | 4      | Number of bytes `N` |
| `char8[N]` | `N`    | UTF8 characters.    |

## Blob Heap

Stores arbitrary chunks of encoded data. The format depends.

## Platform Table

Each row looks as follows:

| Offset | Length | Type           | Name          |
| ------ | ------ | -------------- | ------------- |
| 0      | 4      | `StringOffset` | Platform Name |

## Framework Table

Each row looks as follows:

| Offset | Length | Type                                           | Name              |
| ------ | ------ | ---------------------------------------------- | ----------------- |
| 0      | 4      | `StringOffset`                                 | Framework Name    |
| 4      | 4      | `BlobOffset` -> `AssemblyOffset[]`             | Assemblies        |
| 8      | 4      | `BlobOffset` -> `(StringOffset, BlobOffset)[]` | Assembly Packs    |
| 12     | 4      | `BlobOffset` -> `(StringOffset, BlobOffset)[]` | Assembly Profiles |

> [!NOTE]
> In case of packs and profiles, the blob offsets in the tuple points to an
> array of `AssemblyOffset`.

## Package Table

Each row looks as follows:

| Offset | Length | Type                                                  | Name            |
| ------ | ------ | ----------------------------------------------------- | --------------- |
| 0      | 4      | `StringOffset`                                        | Package Name    |
| 4      | 4      | `StringOffset`                                        | Package Version |
| 8      | 4      | `BlobOffset` -> `(FrameworkOffset, AssemblyOffset)[]` | Assemblies      |

## Assembly Table

Each row looks as follows:

| Offset | Length | Type                                                 | Name           |
| ------ | ------ | ---------------------------------------------------- | -------------- |
| 0      | 16     | `GUID`                                               | Fingerprint    |
| 16     | 4      | `StringOffset`                                       | Name           |
| 20     | 4      | `StringOffset`                                       | PublicKeyToken |
| 24     | 4      | `StringOffset`                                       | Version        |
| 28     | 4      | `BlobOffset` -> `ApiOffset[]`                        | Root APIs      |
| 32     | 4      | `BlobOffset` -> `FrameworkOffset[]`                  | Frameworks     |
| 36     | 8      | `BlobOffset` -> `(PackageOffset, FrameworkOffset)[]` | Packages       |

## API Table

Each row looks as follows:

| Offset | Length | Type                                             | Name         |
| ------ | ------ | ------------------------------------------------ | ------------ |
| 0      | 16     | `GUID`                                           | Fingerprint  |
| 16     | 1      | `Byte`                                           | API kind     |
| 17     | 4      | `ApiOffset`                                      | Parent       |
| 21     | 4      | `StringOffset`                                   | Name         |
| 25     | 4      | `BlobOffset` -> `ApiOffset[]`                    | Children     |
| 29     | 4      | `BlobOffset` -> `(AssemblyOffset, BlobOffset)[]` | Declarations |

## Root API Table

Each row looks as follows:

| Offset | Length | Type        | Name |
| ------ | ------ | ----------- | ---- |
| 0      | 4      | `ApiOffset` | API  |

## Extension Methods Table

Each row looks as follows:

| Offset | Length | Type        | Name                  |
| ------ | ------ | ----------- | --------------------- |
| 0      | 4      | `GUID`      | Extension Method GUID |
| 4      | 4      | `ApiOffset` | Extended Type         |
| 8      | 4      | `ApiOffset` | Extension Method      |

## Obsoletion Table

Each row looks as follows:

| Offset | Length | Type             | Name          |
| ------ | ------ | ---------------- | ------------- |
| 0      | 4      | `ApiOffset`      | API           |
| 4      | 4      | `AssemblyOffset` | Assembly      |
| 8      | 4      | `StringOffset`   | Message       |
| 12     | 1      | `Boolean`        | Is Error      |
| 13     | 4      | `StringOffset`   | Diagnostic ID |
| 17     | 4      | `StringOffset`   | URL Format    |

- The rows are sorted by API and Assembly, to allow binary search based on them.

## Platform Support Table

Each row looks as follows:

| Offset | Length | Type                                        | Name         |
| ------ | ------ | ------------------------------------------- | ------------ |
| 0      | 4      | `ApiOffset`                                 | API          |
| 4      | 4      | `AssemblyOffset`                            | Assembly     |
| 8      | 4      | `BlobOffset` -> `(StringOffset, Boolean)[]` | Support      |

- The rows are sorted by API and Assembly, to allow binary search based on them.

## Preview Requirement Table

Each row looks as follows:

| Offset | Length | Type             | Name     |
| ------ | ------ | ---------------- | -------- |
| 0      | 4      | `ApiOffset`      | API      |
| 4      | 4      | `AssemblyOffset` | Assembly |
| 8      | 4      | `StringOffset`   | Message  |
| 12     | 4      | `StringOffset`   | URL      |

- The rows are sorted by API and Assembly, to allow binary search based on them.

## Experimental Table

Each row looks as follows:

| Offset | Length | Type             | Name          |
| ------ | ------ | ---------------- | ------------- |
| 0      | 4      | `ApiOffset`      | API           |
| 4      | 4      | `AssemblyOffset` | Assembly      |
| 8      | 4      | `StringOffset`   | Diagnostic ID |
| 12     | 4      | `StringOffset`   | URL Format    |

- The rows are sorted by API and Assembly, to allow binary search based on them.

## Blobs

### Arrays

The most common types of blobs are arrays. Arrays are stored length-prefixed.
The length is stored as an `Int32`. Please note the length is number of
elements, not number of bytes.

Element types can either be simple types or tuples:

- `FrameworkOffset[]`
- `AssemblyOffset[]`
- `ApiOffset[]`
- `(FrameworkOffset, AssemblyOffset)[]`
- `(PackageOffset, FrameworkOffset)[]`
- `(AssemblyOffset, BlobOffset)[]`
- `(StringOffset, Boolean)[]`
- `(StringOffset, BlobOffset)[]`

Tuples are stored in sequence with no padding or length prefix.

### Syntax

The declaration syntax of an API is stored as stream of tokens.

* `Int32` - TokenCount
* Sequence of `Token`

Each `Token` has:

* `Byte` - [Kind](../../src/Terrajobst.ApiCatalog/Markup/MarkupTokenKind.cs)
* If the token kind isn't a keyword or operator token, it's followed by
  - `StringOffset` - Text
* If the token kind is `Reference` then the Text field is followed by an
  `ApiOffset`.

## Types

| Type                | Representation | Comment                                                      |
| ------------------- | -------------- | ------------------------------------------------------------ |
| `GUID`              | `Byte[16]`     | A GUID                                                       |
| `Boolean`           | `Byte`         | A Boolean with `True` being `1`, `0` otherwise               |
| `StringOffset`      | `Int32`        | Points to a length-prefixed string in the [string heap]      |
| `BlobOffset`        | `Int32`        | Points to data in the [blob heap]. Representation depends.   |
| `PlatformOffset`    | `Int32`        | Points to the beginning of a row in the [Platform table]     |
| `FrameworkOffset`   | `Int32`        | Points to the beginning of a row in the [Framework table]    |
| `PackageOffset`     | `Int32`        | Points to the beginning of a row in the [Package table]      |
| `AssemblyOffset`    | `Int32`        | Points to the beginning of a row in the [Assembly table]     |
| `ApiOffset`         | `Int32`        | Points to the beginning of a row in the [API table]          |

[String Heap]: #string-heap
[Blob Heap]: #blob-heap
[Platform Table]: #platform-table
[Framework Table]: #framework-table
[Package Table]: #package-table
[Assembly Table]: #assembly-table
[API Table]: #api-table
[Root API Table]: #root-api-table
[Obsoletion Table]: #obsoletion-table
[Platform Support Table]: #platform-support-table
[Preview Requirement Table]: #preview-requirement-table
[Experimental Table]: #experimental-table
[Extension Methods Table]: #extension-methods-table

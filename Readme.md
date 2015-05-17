# CS2TS - CSharp to TypeScript #
CS2TS is a simple program that takes C# source as input and produces a typescript module or definition file with types suitable for use with Json serialized data from the input CSharp types.
It can be downloaded from nuget at https://www.nuget.org/packages/CS2TS

## Objectives ##
 * Produce proper TypeScript definitions from POD classes, structures, and interfaces.
 * Full compatibility with Json.NET and DataContract attributes for naming members.

## Example ##
This is a sample of C# and its generated type script.

```CSharp
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Foo
{
  public struct Point
  {
    public int X, Y;
  }

  public interface IColor
  {
    [JsonProperty("value")]
    string Value { get; set; }
  }

  public class Square
  {
    public Point TopRight { get; set; }
    public Point LowerLeft { get; set; }
    public IColor Fill { get; set; }
  }

  [DataContract]
  public class Path
  {
    [DataMember(Name = "points")]
    public List<Point> Points { get; set; }
  }
}

```
```TypeScript
declare interface Point {
  X: number;
  Y: number;
}

declare interface IColor {
  value?: string;
}

declare interface Square {
  TopRight: Point;
  LowerLeft: Point;
  Fill?: IColor;
}

declare interface Path {
  points?: Point[];
}
```

## Usage ##
CS2TS can be used from the command line and inside visual studio.

### Using From VS ###
Just add the CS2TS nuget package to the project that you want to generate typescript in. You can then specify the inputs and output using MSBuild properties and metadata.

Inputs can be specified by adding `ProcessToTypescript` metadata to the `Compile` items for the C# files you want processed. Alternatively, add `CS2TSInputFile` items to your project for each file you want processed.

The output file can be specified by setting the `CS2TSTypeScriptOutput` property to the target file path. This property defaults to `~/Scripts/generatedTypes.ts`

### Using From the Command Line ###
Run `CS2TS.exe` with the following command line.
```
CS2TS.exe -o <outputFile> [-r <assemblyReferencePath1> [-r <assemblyReferencePath2>]...] <inputFile1> <inputFile2> ...
```

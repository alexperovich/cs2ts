using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CS2TS
{
  public class TypeScriptProcessor
  {
    private readonly string[] _inputs;
    private readonly string[] _referencePaths;
    private static readonly string[] EmptyStrings = new string[0];

    public TypeScriptProcessor(string[] inputFileTexts)
      : this(inputFileTexts, EmptyStrings)
    {
    }

    public TypeScriptProcessor(string[] inputFileTexts, string[] referencePaths)
    {
      _inputs = inputFileTexts;
      _referencePaths = referencePaths;
    }

    public TypeScriptProcessor(string inputFileText)
      : this(new []{inputFileText})
    {
    }

    public void Write(TextWriter writer)
    {
      Write(writer, false);
    }

    public void Write(TextWriter writer, bool declarations)
    {
      var references =
        _referencePaths.Select(p => MetadataReference.CreateFromFile(p))
          .Concat(new[] {MetadataReference.CreateFromAssembly(typeof (object).Assembly)})
          .ToArray();
      var syntaxTrees = _inputs.Select(input => CSharpSyntaxTree.ParseText(input, CSharpParseOptions.Default)).ToArray();
      var compilation = CSharpCompilation.Create("Test", syntaxTrees, references);
      foreach (var tree in syntaxTrees)
      {
        var model = compilation.GetSemanticModel(tree);
        var collector = new TypeCollector();
        collector.Visit(tree.GetRoot());
        var emitter = new TypeScriptEmitter(writer, model, declarations);
        foreach (var type in collector.Types)
        {
          emitter.Emit(type);
        }
        foreach (var e in collector.Enums)
        {
          emitter.Emit(e);
        }
      }
    }

    public string GetTypescriptAsString()
    {
      return GetTypescriptAsString(false);
    }

    public string GetTypescriptAsString(bool declarations)
    {
      using (var writer = new StringWriter())
      {
        Write(writer, declarations);
        return writer.ToString();
      }
    }
  }
}
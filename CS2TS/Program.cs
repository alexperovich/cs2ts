using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CS2TS
{
  internal class ClassCollector : CSharpSyntaxWalker
  {
    public readonly List<TypeDeclarationSyntax> Types = new List<TypeDeclarationSyntax>();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
      Types.Add(node);
      base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
      Types.Add(node);
      base.VisitStructDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
      Types.Add(node);
      base.VisitInterfaceDeclaration(node);
    }
  }

  internal class TypeScriptMemberEmitter : CSharpSyntaxWalker
  {
    private readonly TextWriter _output;
    private readonly SemanticModel _semanticModel;
    private readonly bool _isDeclaration;

    public TypeScriptMemberEmitter(TextWriter output, SemanticModel semanticModel, bool isDeclaration)
    {
      _output = output;
      _semanticModel = semanticModel;
      _isDeclaration = isDeclaration;
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
      var propertySymbol = _semanticModel.GetDeclaredSymbol(node);
      _output.WriteLine("  {0}{2}: {1};", GetPropertyName(propertySymbol), GetTypescriptType(propertySymbol.Type), IsNullable(propertySymbol.Type) ? "?" : "");
      base.VisitPropertyDeclaration(node);
    }

    private string GetPropertyName(IPropertySymbol propertySymbol)
    {
      var name = propertySymbol.Name;
      var attributes = propertySymbol.GetAttributes();
      var jsonPropertyAttribute =
        attributes
          .FirstOrDefault(a => a.AttributeClass.Name == "JsonPropertyAttribute");
      if (jsonPropertyAttribute != null)
      {
        if (jsonPropertyAttribute.ConstructorArguments.Length > 0)
        {
          var firstArgument = jsonPropertyAttribute.ConstructorArguments.First();
          if (firstArgument.Type.SpecialType == SpecialType.System_String)
          {
            name = (string)firstArgument.Value;
          }
        }
      }
      var dataMemberAttribute = attributes.FirstOrDefault(a => a.AttributeClass.Name == "DataMemberAttribute");
      if (dataMemberAttribute != null)
      {
        var namedArguments = dataMemberAttribute.NamedArguments;
        if (namedArguments.Length > 0 && namedArguments.Any(p => p.Key == "Name"))
        {
          var nameArgument = namedArguments.First(p => p.Key == "Name");
          if (nameArgument.Value.Type.SpecialType == SpecialType.System_String)
          {
            name = (string) nameArgument.Value.Value;
          }
        }
      }
      return name;
    }

    private string GetTypescriptType(ITypeSymbol type)
    {
      if (type.TypeKind == TypeKind.Array)
      {
        IArrayTypeSymbol arr = (IArrayTypeSymbol) type;
        return GetTypescriptType(arr.ElementType) + "[]";
      }
      switch (type.SpecialType)
      {
        case SpecialType.System_Object:
          return "any";
        case SpecialType.System_Boolean:
          return "boolean";
        case SpecialType.System_Char:
        case SpecialType.System_SByte:
        case SpecialType.System_Byte:
        case SpecialType.System_Int16:
        case SpecialType.System_UInt16:
        case SpecialType.System_Int32:
        case SpecialType.System_UInt32:
        case SpecialType.System_Int64:
        case SpecialType.System_UInt64:
        case SpecialType.System_Decimal:
        case SpecialType.System_Single:
        case SpecialType.System_Double:
          return "number";
        case SpecialType.System_String:
          return "string";
        case SpecialType.System_Collections_IEnumerable:
          return "any[]";
      }
      switch (type.OriginalDefinition.SpecialType)
      {
        case SpecialType.System_Collections_Generic_IEnumerable_T:
        case SpecialType.System_Collections_Generic_ICollection_T:
        case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
        case SpecialType.System_Collections_Generic_IList_T:
        case SpecialType.System_Collections_Generic_IReadOnlyList_T:
          var namedType = (INamedTypeSymbol) type;
          return GetTypescriptType(namedType.TypeArguments.First()) + "[]";
        default:
          if (_semanticModel.Compilation.Assembly.TypeNames.Any(tn => type.Name == tn))
          {
            return type.Name;
          }
          return type.AllInterfaces.Select(GetTypescriptType).FirstOrDefault(t => t != "any") ?? "any";
      }
    }

    private static bool IsNullable(ITypeSymbol type)
    {
      return type.IsReferenceType;
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
      var declaration = node.Declaration;
      foreach (var variable in declaration.Variables)
      {
        var field = (IFieldSymbol) _semanticModel.GetDeclaredSymbol(variable);
        var type = field.Type;
        _output.WriteLine("  {0}{2}: {1};", variable.Identifier.Text, GetTypescriptType(type), IsNullable(type) ? "?" : "");
      }
      base.VisitFieldDeclaration(node);
    }
  }

  internal struct TypeScriptEmitter
  {
    private readonly TextWriter _output;
    private readonly SemanticModel _semanticModel;
    private readonly bool _isDeclaration;

    public TypeScriptEmitter(TextWriter output, SemanticModel semanticModel, bool isDeclaration)
    {
      _output = output;
      _semanticModel = semanticModel;
      _isDeclaration = isDeclaration;
    }

    public void Emit(TypeDeclarationSyntax typeDeclarationSyntax)
    {
      _output.WriteLine("{1} interface {0} {{", typeDeclarationSyntax.Identifier.Text, _isDeclaration ? "declare" : "export");
      var emitter = new TypeScriptMemberEmitter(_output, _semanticModel, _isDeclaration);
      emitter.Visit(typeDeclarationSyntax);
      _output.WriteLine("}");
      _output.WriteLine();
    }
  }

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
        var collector = new ClassCollector();
        collector.Visit(tree.GetRoot());
        var emitter = new TypeScriptEmitter(writer, model, declarations);
        foreach (var type in collector.Types)
        {
          emitter.Emit(type);
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

  class Program
  {
    static void Main(string[] args)
    {
      var options = ParseOptions(args);
      using (var output = new StreamWriter(new FileStream(options.OutputFile, FileMode.Create)))
      {
        var generateDeclarations = options.OutputFile.EndsWith(".d.ts");
        var inputs = options.InputFiles.Select(File.ReadAllText).ToArray();
        var processor = new TypeScriptProcessor(inputs, options.ReferencePaths.ToArray());
        processor.Write(output, generateDeclarations);
      }
    }

    private static Options ParseOptions(string[] args)
    {
      var ret = new Options();
      int length = args.Length;
      for (int i = 0; i < length; ++i)
      {
        var arg = args[i];
        if (arg[0] == '-')
        {
          i += ret.ProcessOption(arg.Substring(1), args, i + 1);
        }
        else
        {
          ret.ProcessPositionalParameter(arg);
        }
      }
      return ret;
    }
  }

  internal class Options
  {
    private readonly List<string> _inputFiles = new List<string>();
    private readonly List<string> _referencePaths = new List<string>();

    public List<string> InputFiles
    {
      get { return _inputFiles; }
    }

    public string OutputFile { get; set; }

    public List<string> ReferencePaths
    {
      get { return _referencePaths; }
    }

    public int ProcessOption(string optionName, string[] arguments, int paramIndex)
    {
      switch (optionName.ToLowerInvariant())
      {
        case "o":
        case "output":
          OutputFile = Path.GetFullPath(arguments[paramIndex]);
          return 1;
        case "r":
        case "reference":
          ReferencePaths.Add(Path.GetFullPath(arguments[paramIndex]));
          return 1;

      }
      throw new FormatException(string.Format("Unexpected option '{0}'", optionName));
    }

    public void ProcessPositionalParameter(string parameter)
    {
      InputFiles.Add(Path.GetFullPath(parameter));
    }
  }
}

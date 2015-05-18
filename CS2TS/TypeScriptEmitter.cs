using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CS2TS
{
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
      using (var propertyWriter = new StringWriter())
      {
        var emitter = new TypeScriptMemberEmitter(propertyWriter, _semanticModel, _isDeclaration);
        emitter.Visit(typeDeclarationSyntax);
        _output.WriteLine(
          "{1} interface {0}{2} {{",
          typeDeclarationSyntax.Identifier.Text,
          _isDeclaration ? "declare" : "export",
          emitter.Extends);
        _output.Write(propertyWriter.ToString());
        _output.WriteLine("}");
        _output.WriteLine();
      }
    }
  }
}
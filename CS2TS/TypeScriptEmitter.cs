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
      _output.WriteLine("{1} interface {0} {{", typeDeclarationSyntax.Identifier.Text, _isDeclaration ? "declare" : "export");
      var emitter = new TypeScriptMemberEmitter(_output, _semanticModel, _isDeclaration);
      emitter.Visit(typeDeclarationSyntax);
      _output.WriteLine("}");
      _output.WriteLine();
    }
  }
}
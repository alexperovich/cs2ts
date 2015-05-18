using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace CS2TS
{
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

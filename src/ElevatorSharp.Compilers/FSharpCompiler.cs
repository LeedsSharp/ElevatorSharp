using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SimpleSourceCodeServices;
using Microsoft.FSharp.Core;

namespace ElevatorSharp.Compilers
{
	public class FSharpCompiler : PlayerCompiler
	{
		public override CompilationResult Compile(string source)
		{
			var fileName = Path.GetTempFileName();
			var sourceFileName = Path.ChangeExtension(fileName, ".fs");
			var assemblyFileName = Path.ChangeExtension(fileName, ".dll");
			var sourceCodeServices = new SimpleSourceCodeServices();

			File.WriteAllText(sourceFileName, source);

			return
				ToResult(
					sourceCodeServices
						.CompileToDynamicAssembly(
							FscArguments(assemblyFileName,
								sourceFileName,
								source),
							FSharpOption<Tuple<TextWriter, TextWriter>>.None));
		}

		private CompilationResult ToResult(
			Tuple<FSharpErrorInfo[], int, FSharpOption<Assembly>> compilation)
		{
			if (compilation.Item3 == null || compilation.Item3.Equals(FSharpOption<Assembly>.None))
				return CompilationResult.Failed(
					compilation
						.Item1
						.Select(errorInfo => errorInfo.ToString()));

			return CompilationResult.Complete(compilation.Item3.Value);
		}

		private string[] FscArguments(
			string assemblyFileName, 
			string sourceFileName,
			string source)
		{
			return
				new[]
				{
					"-o",
					assemblyFileName,
					"-a",
					sourceFileName
				}
					.Concat(
						GetReferencedAssemblies(
							() => Regex.Matches(source, @"open ([^\s]+)").Cast<Match>().Select(m => m.Groups[1].Value.Trim()))
							.SelectMany(reference => new[]
							{
								"-r",
								reference.Display
							}))
					.ToArray();
		}
	}
}

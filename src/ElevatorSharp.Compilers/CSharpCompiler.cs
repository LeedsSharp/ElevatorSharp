using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ElevatorSharp.Compilers
{
	public class CSharpCompiler : PlayerCompiler
	{
		public override CompilationResult Compile(string source)
		{
			var syntaxTree = CSharpSyntaxTree.ParseText(source);

			var references =
				GetReferencedAssemblies(() => Regex.Matches(source, "using ([^;]+)").Cast<Match>().Select(m => m.Groups[1].Value));
			var compilation = CSharpCompilation.Create("IPlayer", new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

			using (var ms = new MemoryStream())
			{
				var result = compilation.Emit(ms);
				if (result.Success)
				{
					ms.Seek(0, SeekOrigin.Begin);
					return CompilationResult.Complete(Assembly.Load(ms.ToArray()));
				}

				return CompilationResult.Failed(
					result
						.Diagnostics
						.Select(diagnostic => diagnostic.ToString()));
			}
		}

		protected override IEnumerable<MetadataReference> GetReferencedAssemblies(Func<IEnumerable<string>> referenceListFunc)
		{
			return
				new[]
				{
					MetadataReference.CreateFromFile(Assembly.LoadWithPartialName("System").Location),
					MetadataReference.CreateFromFile(Assembly.LoadWithPartialName("System.Core").Location),
					MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
				}
					.Concat(
						base.GetReferencedAssemblies(referenceListFunc));
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ElevatorSharp.Compilers
{
	public abstract class PlayerCompiler
	{
		public abstract CompilationResult Compile(string source);

		public static PlayerCompiler CSharp => new CSharpCompiler();

		public static PlayerCompiler FSharp => new FSharpCompiler();

		protected virtual IEnumerable<MetadataReference> GetReferencedAssemblies(
			Func<IEnumerable<string>> referenceListFunc)
		{
			return
				new[]
				{
					MetadataReference.CreateFromFile(Assembly.LoadWithPartialName("ElevatorSharp.Game").Location),
				}
					.Concat(
						referenceListFunc()
							.Select(reference =>
								Assembly.LoadWithPartialName(reference))
							.Where(assembly => assembly != null)
							.Select(assembly => MetadataReference.CreateFromFile(assembly.Location)));
		}
	}
}

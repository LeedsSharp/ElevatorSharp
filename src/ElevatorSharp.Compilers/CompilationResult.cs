using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorSharp.Compilers
{
	public class CompilationResult
	{
		private CompilationResult(IEnumerable<string> diagnostics)
		{
			Success = false;
			Diagnostics = diagnostics;
		}

		private CompilationResult(
			Assembly assembly)
		{
			Success = true;
			Assembly = assembly;
			Diagnostics = Enumerable.Empty<string>();
		}

		public bool Success { get; private set; }

		public Assembly Assembly { get; private set; }

		public IEnumerable<string> Diagnostics { get; private set; } 

		public static CompilationResult Failed(IEnumerable<string> diagnostics)
		{
			return new CompilationResult(diagnostics);
		}

		public static CompilationResult Complete(
			Assembly assembly)
		{
			return new CompilationResult(assembly);
		}
	}
}

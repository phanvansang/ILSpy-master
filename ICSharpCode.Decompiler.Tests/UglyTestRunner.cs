﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ICSharpCode.Decompiler.Tests.Helpers;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	[TestFixture, Parallelizable(ParallelScope.All)]
	public class UglyTestRunner
	{
		static readonly string TestCasePath = Tester.TestCasePath + "/Ugly";

		[Test]
		public void AllFilesHaveTests()
		{
			var testNames = GetType().GetMethods()
				.Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Any())
				.Select(m => m.Name)
				.ToArray();
			foreach (var file in new DirectoryInfo(TestCasePath).EnumerateFiles()) {
				if (file.Extension.Equals(".il", StringComparison.OrdinalIgnoreCase)
					|| file.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)) {
					var testName = file.Name.Split('.')[0];
					Assert.Contains(testName, testNames);
				}
			}
		}

		static readonly CSharpCompilerOptions[] noRoslynOptions =
		{
			CSharpCompilerOptions.None,
			CSharpCompilerOptions.Optimize
		};

		static readonly CSharpCompilerOptions[] roslynOnlyOptions =
		{
			CSharpCompilerOptions.UseRoslyn,
			CSharpCompilerOptions.Optimize | CSharpCompilerOptions.UseRoslyn
		};

		static readonly CSharpCompilerOptions[] defaultOptions =
		{
			CSharpCompilerOptions.None,
			CSharpCompilerOptions.Optimize,
			CSharpCompilerOptions.UseRoslyn,
			CSharpCompilerOptions.Optimize | CSharpCompilerOptions.UseRoslyn
		};

		[Test]
		public void NoArrayInitializers([ValueSource("roslynOnlyOptions")] CSharpCompilerOptions cscOptions)
		{
			RunForLibrary(cscOptions: cscOptions, decompilerSettings: new DecompilerSettings {
				ArrayInitializers = false
			});
		}

		[Test]
		public void NoDecimalConstants([ValueSource("roslynOnlyOptions")] CSharpCompilerOptions cscOptions)
		{
			RunForLibrary(cscOptions: cscOptions, decompilerSettings: new DecompilerSettings {
				DecimalConstants = false
			});
		}

		void RunForLibrary([CallerMemberName] string testName = null, AssemblerOptions asmOptions = AssemblerOptions.None, CSharpCompilerOptions cscOptions = CSharpCompilerOptions.None, DecompilerSettings decompilerSettings = null)
		{
			Run(testName, asmOptions | AssemblerOptions.Library, cscOptions | CSharpCompilerOptions.Library, decompilerSettings);
		}

		void Run([CallerMemberName] string testName = null, AssemblerOptions asmOptions = AssemblerOptions.None, CSharpCompilerOptions cscOptions = CSharpCompilerOptions.None, DecompilerSettings decompilerSettings = null)
		{
			var ilFile = Path.Combine(TestCasePath, testName) + Tester.GetSuffix(cscOptions) + ".il";
			var csFile = Path.Combine(TestCasePath, testName + ".cs");
			var expectedFile = Path.Combine(TestCasePath, testName + ".Expected.cs");

			if (!File.Exists(ilFile)) {
				// re-create .il file if necessary
				CompilerResults output = null;
				try {
					output = Tester.CompileCSharp(csFile, cscOptions);
					Tester.Disassemble(output.PathToAssembly, ilFile, asmOptions);
				} finally {
					if (output != null)
						output.TempFiles.Delete();
				}
			}

			var executable = Tester.AssembleIL(ilFile, asmOptions);
			var decompiled = Tester.DecompileCSharp(executable, decompilerSettings);
			
			CodeAssert.FilesAreEqual(expectedFile, decompiled, Tester.GetPreprocessorSymbols(cscOptions).ToArray());
		}
	}
}

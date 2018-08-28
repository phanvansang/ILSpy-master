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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ICSharpCode.Decompiler.Tests.Helpers;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	[TestFixture, Parallelizable(ParallelScope.All)]
	public class VBPrettyTestRunner
	{
		static readonly string TestCasePath = Tester.TestCasePath + "/VBPretty";

		[Test]
		public void AllFilesHaveTests()
		{
			var testNames = typeof(VBPrettyTestRunner).GetMethods()
				.Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Any())
				.Select(m => m.Name)
				.ToArray();
			foreach (var file in new DirectoryInfo(TestCasePath).EnumerateFiles()) {
				if (file.Extension.Equals(".vb", StringComparison.OrdinalIgnoreCase)) {
					var testName = file.Name.Split('.')[0];
					Assert.Contains(testName, testNames);
					Assert.IsTrue(File.Exists(Path.Combine(TestCasePath, testName + ".cs")));
				}
			}
		}

		static readonly VBCompilerOptions[] defaultOptions =
{
			VBCompilerOptions.None,
			VBCompilerOptions.Optimize,
			VBCompilerOptions.UseRoslyn,
			VBCompilerOptions.Optimize | VBCompilerOptions.UseRoslyn,
		};

		static readonly VBCompilerOptions[] roslynOnlyOptions =
{
			VBCompilerOptions.UseRoslyn,
			VBCompilerOptions.Optimize | VBCompilerOptions.UseRoslyn,
		};

		[Test, Ignore("Implement VB async/await")]
		public void Async([ValueSource("defaultOptions")] VBCompilerOptions options)
		{
			Run(options: options);
		}

		void Run([CallerMemberName] string testName = null, VBCompilerOptions options = VBCompilerOptions.UseDebug, DecompilerSettings settings = null)
		{
			var vbFile = Path.Combine(TestCasePath, testName + ".vb");
			var csFile = Path.Combine(TestCasePath, testName + ".cs");

			var executable = Tester.CompileVB(vbFile, options);
			var decompiled = Tester.DecompileCSharp(executable.PathToAssembly, settings);

			CodeAssert.FilesAreEqual(csFile, decompiled);
		}
	}
}

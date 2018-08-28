﻿// Copyright (c) 2016 Daniel Grunwald
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Threading;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Tests.Helpers;
using Microsoft.Win32;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	[TestFixture, Parallelizable(ParallelScope.All)]
	public class RoundtripAssembly
	{
		public static readonly string TestDir = Path.GetFullPath(Path.Combine(Tester.TestCasePath, "../../ILSpy-tests"));
		static readonly string nunit = Path.Combine(TestDir, "nunit", "nunit3-console.exe");
		
		[Test]
		public void Cecil_net45()
		{
			RunWithTest("Mono.Cecil-net45", "Mono.Cecil.dll", "Mono.Cecil.Tests.dll");
		}

		[Test]
		public void NewtonsoftJson_net45()
		{
			RunWithTest("Newtonsoft.Json-net45", "Newtonsoft.Json.dll", "Newtonsoft.Json.Tests.dll");
		}

		[Test]
		public void NewtonsoftJson_pcl_debug()
		{
			try {
				RunWithTest("Newtonsoft.Json-pcl-debug", "Newtonsoft.Json.dll", "Newtonsoft.Json.Tests.dll");
			} catch (CompilationFailedException) {
				Assert.Ignore("Cannot yet re-compile PCL projects.");
			}
		}

		[Test]
		public void NRefactory_CSharp()
		{
			RunWithTest("NRefactory", "ICSharpCode.NRefactory.CSharp.dll", "ICSharpCode.NRefactory.Tests.dll");
		}

		[Test]
		public void ICSharpCode_Decompiler()
		{
			try {
				RunWithTest("ICSharpCode.Decompiler", "ICSharpCode.Decompiler.dll", "ICSharpCode.Decompiler.Tests.exe");
			} catch (CompilationFailedException) {
				Assert.Ignore("C# 7 local functions not yet supported.");
			}
		}

		[Test]
		public void ImplicitConversions()
		{
			RunWithOutput("Random Tests\\TestCases", "ImplicitConversions.exe");
		}

		[Test]
		public void ImplicitConversions_32()
		{
			RunWithOutput("Random Tests\\TestCases", "ImplicitConversions_32.exe");
		}

		[Test]
		public void ExplicitConversions()
		{
			RunWithOutput("Random Tests\\TestCases", "ExplicitConversions.exe");
		}

		[Test]
		public void ExplicitConversions_32()
		{
			RunWithOutput("Random Tests\\TestCases", "ExplicitConversions_32.exe");
		}

		[Test]
		public void Random_TestCase_1()
		{
			RunWithOutput("Random Tests\\TestCases", "TestCase-1.exe");
		}

		void RunWithTest(string dir, string fileToRoundtrip, string fileToTest)
		{
			RunInternal(dir, fileToRoundtrip, outputDir => RunTest(outputDir, fileToTest));
		}
		
		void RunWithOutput(string dir, string fileToRoundtrip)
		{
			string inputDir = Path.Combine(TestDir, dir);
			RunInternal(dir, fileToRoundtrip,
				outputDir => Tester.RunAndCompareOutput(fileToRoundtrip, Path.Combine(inputDir, fileToRoundtrip), Path.Combine(outputDir, fileToRoundtrip)));
		}
		
		void RunInternal(string dir, string fileToRoundtrip, Action<string> testAction)
		{
			if (!Directory.Exists(TestDir)) {
				Assert.Ignore($"Assembly-roundtrip test ignored: test directory '{TestDir}' needs to be checked out separately." + Environment.NewLine +
				              $"git clone https://github.com/icsharpcode/ILSpy-tests \"{TestDir}\"");
			}
			string inputDir = Path.Combine(TestDir, dir);
			string decompiledDir = inputDir + "-decompiled";
			string outputDir = inputDir + "-output";
			if (inputDir.EndsWith("TestCases")) {
				// make sure output dir names are unique so that we don't get trouble due to parallel test execution
				decompiledDir += Path.GetFileNameWithoutExtension(fileToRoundtrip);
				outputDir += Path.GetFileNameWithoutExtension(fileToRoundtrip);
			}
			ClearDirectory(decompiledDir);
			ClearDirectory(outputDir);
			string projectFile = null;
			foreach (string file in Directory.EnumerateFiles(inputDir, "*", SearchOption.AllDirectories)) {
				if (!file.StartsWith(inputDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) {
					Assert.Fail($"Unexpected file name: {file}");
				}
				string relFile = file.Substring(inputDir.Length + 1);
				Directory.CreateDirectory(Path.Combine(outputDir, Path.GetDirectoryName(relFile)));
				if (relFile.Equals(fileToRoundtrip, StringComparison.OrdinalIgnoreCase)) {
					Console.WriteLine($"Decompiling {fileToRoundtrip}...");
					Stopwatch w = Stopwatch.StartNew();
					using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read)) {
						PEFile module = new PEFile(file, fileStream, PEStreamOptions.PrefetchEntireImage);
						var resolver = new UniversalAssemblyResolver(file, false, module.Reader.DetectTargetFrameworkId(), PEStreamOptions.PrefetchMetadata);
						resolver.AddSearchDirectory(inputDir);
						resolver.RemoveSearchDirectory(".");
						var decompiler = new TestProjectDecompiler(inputDir);
						decompiler.AssemblyResolver = resolver;
						// use a fixed GUID so that we can diff the output between different ILSpy runs without spurious changes
						decompiler.ProjectGuid = Guid.Parse("{127C83E4-4587-4CF9-ADCA-799875F3DFE6}");
						decompiler.DecompileProject(module, decompiledDir);
						Console.WriteLine($"Decompiled {fileToRoundtrip} in {w.Elapsed.TotalSeconds:f2}");
						projectFile = Path.Combine(decompiledDir, module.Name + ".csproj");
					}
				} else {
					File.Copy(file, Path.Combine(outputDir, relFile));
				}
			}
			Assert.IsNotNull(projectFile, $"Could not find {fileToRoundtrip}");
			
			Compile(projectFile, outputDir);
			testAction(outputDir);
		}

		static void ClearDirectory(string dir)
		{
			Directory.CreateDirectory(dir);
			foreach (string subdir in Directory.EnumerateDirectories(dir)) {
				for (int attempt = 0; ; attempt++) {
					try {
						Directory.Delete(subdir, true);
						break;
					} catch (IOException) {
						if (attempt >= 10)
							throw;
						Thread.Sleep(100);
					}
				}
			}
			foreach (string file in Directory.EnumerateFiles(dir)) {
				File.Delete(file);
			}
		}
		
		static string FindVS2017()
		{
			using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)) {
				using (var subkey = key.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\SxS\VS7")) {
					return subkey?.GetValue("15.0") as string;
				}
			}
		}

		static string FindMSBuild()
		{
			string vsPath = FindVS2017();
			if (vsPath == null)
				throw new InvalidOperationException("Could not find VS2017");
			return Path.Combine(vsPath, @"MSBuild\15.0\bin\MSBuild.exe");
		}

		static void Compile(string projectFile, string outputDir)
		{
			var info = new ProcessStartInfo(FindMSBuild());
			info.Arguments = $"/nologo /v:minimal /p:OutputPath=\"{outputDir}\" \"{projectFile}\"";
			info.CreateNoWindow = true;
			info.UseShellExecute = false;
			info.RedirectStandardOutput = true;
			// Don't let environment variables (e.g. set by AppVeyor) influence the build.
			info.EnvironmentVariables.Remove("Configuration");
			info.EnvironmentVariables.Remove("Platform");
			Console.WriteLine($"\"{info.FileName}\" {info.Arguments}");
			using (var p = Process.Start(info)) {
				Regex errorRegex = new Regex(@"^[\w\d.\\-]+\(\d+,\d+\):");
				string suffix = $" [{projectFile}]";
				string line;
				while ((line = p.StandardOutput.ReadLine()) != null) {
					if (line.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) {
						line = line.Substring(0, line.Length - suffix.Length);
					}
					Match m = errorRegex.Match(line);
					if (m.Success) {
						// Make path absolute so that it gets hyperlinked
						line = Path.GetDirectoryName(projectFile) + Path.DirectorySeparatorChar + line;
					}
					Console.WriteLine(line);
				}
				p.WaitForExit();
				if (p.ExitCode != 0)
					throw new CompilationFailedException($"Compilation of {Path.GetFileName(projectFile)} failed");
			}
		}
		
		static void RunTest(string outputDir, string fileToTest)
		{
			var info = new ProcessStartInfo(nunit);
			info.WorkingDirectory = outputDir;
			info.Arguments = $"\"{fileToTest}\"";
			info.CreateNoWindow = true;
			info.UseShellExecute = false;
			info.RedirectStandardOutput = true;
			Console.WriteLine($"\"{info.FileName}\" {info.Arguments}");
			using (var p = Process.Start(info)) {
				string line;
				while ((line = p.StandardOutput.ReadLine()) != null) {
					Console.WriteLine(line);
				}
				p.WaitForExit();
				if (p.ExitCode != 0)
					throw new TestRunFailedException($"Test execution of {Path.GetFileName(fileToTest)} failed");
			}
		}

		class TestProjectDecompiler : WholeProjectDecompiler
		{
			readonly string[] localAssemblies;

			public TestProjectDecompiler(string baseDir)
			{
				localAssemblies = new DirectoryInfo(baseDir).EnumerateFiles("*.dll").Select(f => f.FullName).ToArray();
			}

			protected override bool IsGacAssembly(IAssemblyReference r, PEFile asm)
			{
				if (asm == null)
					return false;
				return !localAssemblies.Contains(asm.FileName);
			}
		}

		class CompilationFailedException : Exception
		{
			public CompilationFailedException(string message) : base(message)
			{
			}
		}
		
		class TestRunFailedException : Exception
		{
			public TestRunFailedException(string message) : base(message)
			{
			}
		}
	}
}

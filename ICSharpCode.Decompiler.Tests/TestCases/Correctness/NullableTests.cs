﻿// Copyright (c) 2017 Daniel Grunwald
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

namespace ICSharpCode.Decompiler.Tests.TestCases.Correctness
{
	class NullableTests
	{
		static void Main()
		{
			Console.WriteLine("MayThrow:");
			Console.WriteLine(MayThrow(10, 2, 3));
			Console.WriteLine(MayThrow(10, 0, null));

			Console.WriteLine("NotUsingAllInputs:");
			Console.WriteLine(NotUsingAllInputs(5, 3));
			Console.WriteLine(NotUsingAllInputs(5, null));

			Console.WriteLine("UsingUntestedValue:");
			Console.WriteLine(NotUsingAllInputs(5, 3));
			Console.WriteLine(NotUsingAllInputs(5, null));
		}

		static int? MayThrow(int? a, int? b, int? c)
		{
			// cannot be lifted without changing the exception behavior
			return a.HasValue & b.HasValue & c.HasValue ? a.GetValueOrDefault() / b.GetValueOrDefault() + c.GetValueOrDefault() : default(int?);
		}

		static int? NotUsingAllInputs(int? a, int? b)
		{
			// cannot be lifted because the value differs if b == null
			return a.HasValue & b.HasValue ? a.GetValueOrDefault() + a.GetValueOrDefault() : default(int?);
		}

		static int? UsingUntestedValue(int? a, int? b)
		{
			// cannot be lifted because the value differs if b == null
			return a.HasValue ? a.GetValueOrDefault() + b.GetValueOrDefault() : default(int?);
		}
	}
}

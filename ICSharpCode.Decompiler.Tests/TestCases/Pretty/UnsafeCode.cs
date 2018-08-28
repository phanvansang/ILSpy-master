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
using System.Runtime.InteropServices;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	public class UnsafeCode
	{
		public struct SimpleStruct
		{
			public int X;
			public double Y;
		}
		
		public struct StructWithFixedSizeMembers
		{
			public unsafe fixed int Integers[100];
			public int NormalMember;
			public unsafe fixed double Doubles[200];

			[Obsolete("another attribute")]
			public unsafe fixed byte Old[1];
		}

		private struct Data
		{
			public Vector Position;
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct Vector
		{
			public override int GetHashCode()
			{
				return 0;
			}
		}

		public unsafe delegate void UnsafeDelegate(byte* ptr);

		private UnsafeDelegate unsafeDelegate;
		private static UnsafeDelegate staticUnsafeDelegate;

#if CS60
		public unsafe int* NullPointer => null;
#else
		public unsafe int* NullPointer {
			get {
				return null;
			}
		}
#endif

		unsafe static UnsafeCode()
		{
			staticUnsafeDelegate = UnsafeStaticMethod;
		}

		public unsafe UnsafeCode()
		{
			unsafeDelegate = UnsafeMethod;
		}

		public unsafe int SizeOf()
		{
			return sizeof(SimpleStruct);
		}

		private static void UseBool(bool b)
		{
		}

		private unsafe void UnsafeMethod(byte* ptr)
		{

		}

		private unsafe static void UnsafeStaticMethod(byte* ptr)
		{

		}

		public unsafe void PointerComparison(int* a, double* b)
		{
			UseBool(a == b);
			UseBool(a != b);
			UseBool(a < b);
			UseBool(a > b);
			UseBool(a <= b);
			UseBool(a >= b);
		}

		public unsafe void PointerComparisonWithNull(int* a)
		{
			UseBool(a == null);
			UseBool(a != null);
		}

		public unsafe int* PointerCast(long* p)
		{
			return (int*)p;
		}

		public unsafe long ConvertDoubleToLong(double d)
		{
			return *(long*)(&d);
		}

		public unsafe double ConvertLongToDouble(long d)
		{
			return *(double*)(&d);
		}

		public unsafe int ConvertFloatToInt(float d)
		{
			return *(int*)(&d);
		}

		public unsafe float ConvertIntToFloat(int d)
		{
			return *(float*)(&d);
		}

		public unsafe int PointerCasts()
		{
			int result = 0;
			*(float*)(&result) = 0.5f;
			((byte*)(&result))[3] = 3;
			return result;
		}

		public unsafe void PassRefParameterAsPointer(ref int p)
		{
			fixed (int* ptr = &p) {
				UsePointer(ptr);
			}
		}

		public unsafe void PassPointerAsRefParameter(int* p)
		{
			UseReference(ref *p);
		}

		public unsafe void PassPointerCastAsRefParameter(uint* p)
		{
			UseReference(ref *(int*)p);
		}

		public unsafe void AddressInMultiDimensionalArray(double[,] matrix)
		{
			fixed (double* d = &matrix[1, 2]) {
				PointerReferenceExpression(d);
				PointerReferenceExpression(d);
			}
		}
		
		public unsafe void FixedStringAccess(string text)
		{
			fixed (char* ptr = text) {
				for (char* ptr2 = ptr; *ptr2 == 'a'; ptr2++) {
					*ptr2 = 'A';
				}
			}
		}

		public unsafe void FixedStringNoPointerUse(string text)
		{
			fixed (char* ptr = text) {
			}
		}

		public unsafe void PutDoubleIntoLongArray1(long[] array, int index, double val)
		{
			fixed (long* ptr = array) {
				*(double*)(ptr + index) = val;
			}
		}

		public unsafe void PutDoubleIntoLongArray2(long[] array, int index, double val)
		{
			fixed (long* ptr = &array[index]) {
				*(double*)ptr = val;
			}
		}

		public unsafe string PointerReferenceExpression(double* d)
		{
			return d->ToString();
		}

		public unsafe string PointerReferenceExpression2(long addr)
		{
			return ((int*)addr)->ToString();
		}

		public unsafe int* PointerArithmetic(int* p)
		{
			return p + 2;
		}

		public unsafe long* PointerArithmetic2(long* p)
		{
			return 3 + p;
		}

		public unsafe long* PointerArithmetic3(long* p)
		{
			return (long*)((byte*)p + 3);
		}

		public unsafe long* PointerArithmetic4(void* p)
		{
			return (long*)((byte*)p + 3);
		}

		public unsafe int PointerArithmetic5(void* p, byte* q, int i)
		{
			return q[i] + *(byte*)p;
		}

		public unsafe int PointerArithmetic6(SimpleStruct* p, int i)
		{
			return p[i].X;
		}

		public unsafe int* PointerArithmeticLong1(int* p, long offset)
		{
			return p + offset;
		}

		public unsafe int* PointerArithmeticLong2(int* p, long offset)
		{
			return offset + p;
		}

		public unsafe int* PointerArithmeticLong3(int* p, long offset)
		{
			return p - offset;
		}

		public unsafe SimpleStruct* PointerArithmeticLong1s(SimpleStruct* p, long offset)
		{
			return p + offset;
		}

		public unsafe SimpleStruct* PointerArithmeticLong2s(SimpleStruct* p, long offset)
		{
			return offset + p;
		}

		public unsafe SimpleStruct* PointerArithmeticLong3s(SimpleStruct* p, long offset)
		{
			return p - offset;
		}

		public unsafe int PointerSubtraction(long* p, long* q)
		{
			return (int)(p - q);
		}

		public unsafe long PointerSubtractionLong(long* p, long* q)
		{
			return p - q;
		}

		public unsafe int PointerSubtraction2(long* p, short* q)
		{
			return (int)((byte*)p - (byte*)q);
		}

		public unsafe int PointerSubtraction3(void* p, void* q)
		{
			return (int)((byte*)p - (byte*)q);
		}

		public unsafe long PointerSubtraction4(sbyte* p, sbyte* q)
		{
			return p - q;
		}

		public unsafe long PointerSubtraction5(SimpleStruct* p, SimpleStruct* q)
		{
			return p - q;
		}

		public unsafe double FixedMemberAccess(StructWithFixedSizeMembers* m, int i)
		{
			return (double)m->Integers[i] + m->Doubles[i];
		}

		public unsafe double* FixedMemberBasePointer(StructWithFixedSizeMembers* m)
		{
			return m->Doubles;
		}

		public unsafe void UseFixedMemberAsPointer(StructWithFixedSizeMembers* m)
		{
			UsePointer(m->Integers);
		}

		public unsafe void UseFixedMemberAsReference(StructWithFixedSizeMembers* m)
		{
			UseReference(ref *m->Integers);
			UseReference(ref m->Integers[1]);
		}

		public unsafe void PinFixedMember(ref StructWithFixedSizeMembers m)
		{
			fixed (int* ptr = m.Integers) {
				UsePointer(ptr);
			}
		}

		private void UseReference(ref int i)
		{
		}

		public unsafe string UsePointer(int* ptr)
		{
			return ptr->ToString();
		}

		public unsafe string UsePointer(double* ptr)
		{
			return ptr->ToString();
		}

		public unsafe string StackAlloc(int count)
		{
			char* ptr = stackalloc char[count];
			char* ptr2 = stackalloc char[100];
			for (int i = 0; i < count; i++) {
				ptr[i] = (char)i;
				ptr2[i] = '\0';
			}
			return UsePointer((double*)ptr);
		}

		public unsafe string StackAllocStruct(int count)
		{
			SimpleStruct* ptr = stackalloc SimpleStruct[checked(count * 2)];
#if !(ROSLYN && OPT)
			// unused stackalloc gets optimized out by roslyn
			SimpleStruct* ptr2 = stackalloc SimpleStruct[10];
#endif
			ptr->X = count;
			ptr[1].X = ptr->X;
			for (int i = 2; i < 10; i++) {
				ptr[i].X = count;
			}
			return UsePointer(&ptr->Y);
		}

		unsafe ~UnsafeCode()
		{
			PassPointerAsRefParameter(NullPointer);
		}

		private unsafe void Issue990()
		{
			Data data = default(Data);
			Data* ptr = &data;
			ConvertIntToFloat(ptr->Position.GetHashCode());
		}

		private unsafe static void Issue1021(ref byte* bytePtr, ref short* shortPtr)
		{
			bytePtr += 4;
			shortPtr += 2;
			bytePtr -= 4;
			shortPtr = (short*)((byte*)shortPtr - 3);
		}
	}
}

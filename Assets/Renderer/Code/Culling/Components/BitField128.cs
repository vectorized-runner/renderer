using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Renderer
{
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct BitField128
	{
		[FieldOffset(0)]
		public BitField64 Lower;

		[FieldOffset(8)]
		public BitField64 Upper;

		[FieldOffset(0)]
		public fixed ulong Mem[2];

		// This can be used with chunk entity enumerator
		[FieldOffset(0)]
		public v128 v128;

		public BitField128(ulong lowerBits, ulong upperBits)
		{
			v128 = new v128(0);
			Lower = new BitField64(lowerBits);
			Upper = new BitField64(upperBits);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetBits(int pos, bool value, int numBits = 1)
		{
			var idx = pos / 64;
			var newPos = pos - idx * 64;
			ref var asBitField = ref UnsafeUtility.As<ulong, BitField64>(ref Mem[idx]);
			asBitField.SetBits(newPos, value, numBits);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsSet(int pos)
		{
			var idx = pos / 64;
			var newPos = pos - idx * 64;
			ref var asBitField = ref UnsafeUtility.As<ulong, BitField64>(ref Mem[idx]);
			return asBitField.IsSet(newPos);
		}
	}
}
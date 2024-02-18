using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Renderer
{
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct BitField128
	{
		[FieldOffset(0)]
		public BitField64 Lower;

		[FieldOffset(8)]
		public BitField64 Upper;

		// Used for making SetBit branch-less
		[FieldOffset(0)]
		public fixed ulong Mem[2];

		// Used for easy ChunkEntityEnumerator construction
		[FieldOffset(0)]
		public v128 v128;

		public BitField128(v128 value)
		{
			Lower = default;
			Upper = default;
			v128 = value;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetBit(int pos, bool value)
		{
			var idx = pos / 64;
			var newPos = pos - idx * 64;
			ref var asBitField = ref UnsafeUtility.As<ulong, BitField64>(ref Mem[idx]);
			asBitField.SetBits(newPos, value, 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetBitsFromStart(bool value, int numBits)
		{
			if (numBits > 64)
			{
				// 64 -> 0
				// 80 -> 16
				// 128 -> 64
				Upper.SetBits(0, value, numBits - 64);
			}
			
			// 0 -> 0
			// 16 -> 16
			// 64 -> 64
			// 80 -> 64
			// 128 -> 64
			Lower.SetBits(0, value, math.min(64, numBits));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsSet(int pos)
		{
			var idx = pos / 64;
			var newPos = pos - idx * 64;
			ref var asBitField = ref UnsafeUtility.As<ulong, BitField64>(ref Mem[idx]);
			return asBitField.IsSet(newPos);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int CountBits()
		{
			return Lower.CountBits() + Upper.CountBits();
		}
	}
}
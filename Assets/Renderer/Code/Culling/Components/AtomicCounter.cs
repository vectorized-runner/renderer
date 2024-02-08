using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Renderer
{
	public unsafe struct AtomicCounter : IDisposable
	{
		[NoAlias]
		[NativeDisableUnsafePtrRestriction]
		public int* Ptr;
		
		public int Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => *Ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(int amount)
		{
			var location = (long)Ptr;
			Interlocked.Add(ref location, amount);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AtomicCounter Create()
		{
			return new AtomicCounter(UnsafeUtil.MallocPersistentInitialized<int>());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public AtomicCounter(int* ptr)
		{
			Ptr = ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (Ptr != null)
			{
				UnsafeUtility.Free(Ptr, Allocator.Persistent);
				Ptr = null;
			}
		}
	}
}
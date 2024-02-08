using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Renderer
{
	public unsafe struct AtomicInt : IDisposable
	{
		[NoAlias]
		[NativeDisableUnsafePtrRestriction]
		public int* Ptr;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(int amount)
		{
			var location = (long)Ptr;
			Interlocked.Add(ref location, amount);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AtomicInt Create()
		{
			return new AtomicInt(UnsafeUtil.MallocPersistentInitialized<int>());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public AtomicInt(int* ptr)
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
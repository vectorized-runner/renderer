using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Renderer
{
	public readonly unsafe struct AtomicInt : IDisposable
	{
		public readonly int* Ptr;

		public void Add(int amount)
		{
			var location = (long)Ptr;
			Interlocked.Add(ref location, amount);
		}

		public static AtomicInt Create()
		{
			return new AtomicInt(UnsafeUtil.MallocPersistentInitialized<int>());
		}

		public AtomicInt(int* ptr)
		{
			Ptr = ptr;
		}

		public void Dispose()
		{
			UnsafeUtility.Free(Ptr, Allocator.Persistent);
		}
	}
}
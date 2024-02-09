using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace Renderer
{
	[NativeContainer]
	public unsafe struct NativeAtomicCounter : IDisposable
	{
		[NoAlias]
		[NativeDisableUnsafePtrRestriction]
		public int* CounterPerThread;

		public const int IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);
		public const int ThreadCount = JobsUtility.MaxJobThreadCount;

		[NativeSetThreadIndex] public int ThreadIndex;

		public int Count
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				// Verify that the caller has read permission on this data. 
				// This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
				var result = 0;

				for (int i = 0; i < ThreadCount; i++)
				{
					result += CounterPerThread[i * IntsPerCacheLine];
				}

				return result;
			}
			set
			{
				// Verify that the caller has write permission on this data. 
				// This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
				// Clear all locally cached counts, 
				// set the first one to the required value
				for (int i = 1; i < JobsUtility.MaxJobThreadCount; ++i)
				{
					CounterPerThread[IntsPerCacheLine * i] = 0;
				}

				*CounterPerThread = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Increment(int amount = 1)
		{
			CounterPerThread[ThreadIndex * IntsPerCacheLine] += amount;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static NativeAtomicCounter Create()
		{
			var cacheLineSize = JobsUtility.CacheLineSize;
			var memory = (int*)UnsafeUtility.Malloc(cacheLineSize * ThreadCount, cacheLineSize, Allocator.Persistent);

			for (int i = 0; i < ThreadCount; i++)
			{
				memory[i * IntsPerCacheLine] = 0;
			}

			return new NativeAtomicCounter(memory);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public NativeAtomicCounter(int* counterPerThread)
		{
			CounterPerThread = counterPerThread;
			ThreadIndex = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (CounterPerThread != null)
			{
				UnsafeUtility.Free(CounterPerThread, Allocator.Persistent);
				CounterPerThread = null;
			}
		}
	}
}
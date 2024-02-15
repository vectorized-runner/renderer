using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace Renderer
{
	public unsafe struct UnsafeAtomicCounter : IDisposable
	{
		[NoAlias]
		[NativeDisableUnsafePtrRestriction]
		public int* CounterPerThread;

		public Allocator Allocator;

		public const int IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);
		public const int ThreadCount = JobsUtility.MaxJobThreadCount;

		public void Add(int threadIndex, int amount)
		{
			CounterPerThread[threadIndex * IntsPerCacheLine] += amount;
		}

		public int Count
		{
			get
			{
				var result = 0;

				for (int i = 0; i < ThreadCount; i++)
				{
					result += CounterPerThread[i * IntsPerCacheLine];
				}

				return result;
			}
			set
			{
				for (int i = 1; i < JobsUtility.MaxJobThreadCount; ++i)
				{
					CounterPerThread[IntsPerCacheLine * i] = 0;
				}

				*CounterPerThread = value;
			}
		}

		public UnsafeAtomicCounter(Allocator allocator)
		{
			CounterPerThread = (int*)UnsafeUtility.Malloc(JobsUtility.CacheLineSize * ThreadCount,
				JobsUtility.CacheLineSize, allocator);

			Allocator = allocator;
			Count = 0;
		}

		public void Dispose()
		{
			UnsafeUtility.Free(CounterPerThread, Allocator);
			CounterPerThread = null;
		}
	}
}
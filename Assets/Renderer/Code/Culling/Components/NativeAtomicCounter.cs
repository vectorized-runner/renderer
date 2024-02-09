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

#if ENABLE_UNITY_COLLECTIONS_CHECKS
		AtomicSafetyHandle m_Safety;

		// The dispose sentinel tracks memory leaks. It is a managed type so it is cleared to null when scheduling a job
		// The job cannot dispose the container, and no one else can dispose it until the job has run, so it is ok to not pass it along
		// This attribute is required, without it this NativeContainer cannot be passed to a job; since that would give the job access to a managed object
		[NativeSetClassTypeToNullOnSchedule] DisposeSentinel m_DisposeSentinel;
#endif

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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
			
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif
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
			// Let the dispose sentinel know that the data has been freed so it does not report any memory leaks
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
		}
	}
}
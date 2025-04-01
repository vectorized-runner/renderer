using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace Renderer
{
	/// <summary>
	/// Implementation mostly taken from here: https://docs.unity3d.com/Packages/com.unity.jobs@0.2/manual/custom_job_types.html
	/// Idea: Interlocked (Atomic) Add instruction is expensive, so use per-thread ints and accumulate,
	/// but also always leave at least cache-line size between threads so they don't flush the cache (extra optimization)
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	[NativeContainer]
	public unsafe struct NativeAtomicCounter : IDisposable
	{
		[NativeContainer]
		[NativeContainerIsAtomicWriteOnly]
		public struct ParallelWriter
		{
			[NativeDisableUnsafePtrRestriction]
			private int* _counterPerThread;

			[NativeSetThreadIndex]
			public int ThreadIndex;

			// Copy of the AtomicSafetyHandle from the full NativeCounter.
			// The dispose sentinel is not copied since this inner struct does not own the memory and is not responsible for freeing it.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			AtomicSafetyHandle m_Safety;
#endif

			// This is what makes it possible to assign to NativeCounter.Concurrent from NativeCounter
			public static implicit operator ParallelWriter(NativeAtomicCounter cnt)
			{
				ParallelWriter parallelWriter;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				AtomicSafetyHandle.CheckWriteAndThrow(cnt.m_Safety);
				parallelWriter.m_Safety = cnt.m_Safety;
				AtomicSafetyHandle.UseSecondaryVersion(ref parallelWriter.m_Safety);
#endif

				parallelWriter._counterPerThread = cnt.CounterPerThread;
				parallelWriter.ThreadIndex = 0;
				return parallelWriter;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Increment(int amount = 1)
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
				_counterPerThread[ThreadIndex * IntsPerCacheLine] += amount;
			}
		}

		[NoAlias]
		[NativeDisableUnsafePtrRestriction]
		public int* CounterPerThread;

		public const int IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);
		public const int ThreadCount = JobsUtility.MaxJobThreadCount;

		[NativeSetThreadIndex]
		public int ThreadIndex;

		private Allocator _allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
		private AtomicSafetyHandle m_Safety;

		// The dispose sentinel tracks memory leaks. It is a managed type so it is cleared to null when scheduling a job
		// The job cannot dispose the container, and no one else can dispose it until the job has run, so it is ok to not pass it along
		// This attribute is required, without it this NativeContainer cannot be passed to a job; since that would give the job access to a managed object
		[NativeSetClassTypeToNullOnSchedule]
		private DisposeSentinel m_DisposeSentinel;
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
		public NativeAtomicCounter(Allocator allocator)
		{
			CounterPerThread = (int*)UnsafeUtility.Malloc(JobsUtility.CacheLineSize * ThreadCount,
				JobsUtility.CacheLineSize, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
			DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif
			_allocator = allocator;
			ThreadIndex = 0;
			Count = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			// Let the dispose sentinel know that the data has been freed so it does not report any memory leaks
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

			UnsafeUtility.Free(CounterPerThread, _allocator);
			CounterPerThread = null;
		}
	}
}
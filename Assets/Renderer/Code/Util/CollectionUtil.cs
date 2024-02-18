using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Debug = UnityEngine.Debug;

namespace Renderer
{
	public static unsafe class CollectionUtil
	{
		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		private static void CheckElementAccess(int index, int count)
		{
			if (index < 0 || index >= count)
				throw new InvalidOperationException($"Access index '{index}' is out of range of count '{count}'");
		}

		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		private static void CheckElementAccessRange(int index, int length, int count)
		{
			if (length < 0)
				throw new InvalidOperationException($"Invalid length '{length}'");

			CheckElementAccess(index, count);

			if (length > 0)
			{
				CheckElementAccess(index + length - 1, count);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T* GetTypedPtr<T>(this NativeArray<T> array) where T : unmanaged
		{
			return (T*)array.GetUnsafePtr();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T* GetTypedPtr<T>(this NativeList<T> list) where T : unmanaged
		{
			return list.GetUnsafePtr();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T ElementAsRef<T>(this NativeList<T> list, int index) where T : unmanaged
		{
			CheckElementAccess(index, list.Length);
			return ref UnsafeUtility.ArrayElementAsRef<T>(list.GetUnsafePtr(), index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly T ElementAsReadonlyRef<T>(this NativeList<T> list, int index) where T : unmanaged
		{
			CheckElementAccess(index, list.Length);
			return ref UnsafeUtility.ArrayElementAsRef<T>(list.GetUnsafeReadOnlyPtr(), index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T ElementAsRef<T>(this NativeArray<T> array, int index) where T : unmanaged
		{
			CheckElementAccess(index, array.Length);
			return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly T ElementAsReadonlyRef<T>(this NativeArray<T> array, int index) where T : unmanaged
		{
			CheckElementAccess(index, array.Length);
			return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> AsSpan<T>(this UnsafeList<T> list, int startIndex, int length) where T : unmanaged
		{
			CheckElementAccessRange(startIndex, length, list.Length);
			return GetUnsafeSpan(list.Ptr, startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<T> AsReadOnlySpan<T>(this UnsafeList<T> list, int startIndex, int length)
			where T : unmanaged
		{
			CheckElementAccessRange(startIndex, length, list.Length);
			return GetUnsafeReadOnlySpan(list.Ptr, startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> AsSpan<T>(this NativeList<T> list, int startIndex, int length) where T : unmanaged
		{
			CheckElementAccessRange(startIndex, length, list.Length);
			return GetUnsafeSpan(list.GetTypedPtr(), startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<T> AsReadOnlySpan<T>(this NativeList<T> list, int startIndex, int length)
			where T : unmanaged
		{
			CheckElementAccessRange(startIndex, length, list.Length);
			return GetUnsafeReadOnlySpan(list.GetTypedPtr(), startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> AsSpan<T>(this NativeArray<T> array, int startIndex, int length) where T : unmanaged
		{
			CheckElementAccessRange(startIndex, length, array.Length);
			return GetUnsafeSpan(array.GetTypedPtr(), startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<T> AsReadOnlySpan<T>(this NativeArray<T> array, int startIndex, int length)
			where T : unmanaged
		{
			CheckElementAccessRange(startIndex, length, array.Length);
			return GetUnsafeReadOnlySpan(array.GetTypedPtr(), startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<T> GetUnsafeReadOnlySpan<T>(T* ptr, int startIndex, int length) where T : unmanaged
		{
			return new ReadOnlySpan<T>(ptr + startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> GetUnsafeSpan<T>(T* ptr, int startIndex, int length) where T : unmanaged
		{
			return new Span<T>(ptr + startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> AsSpan<T>(this NativeArray<T> array) where T : unmanaged
		{
			return AsSpan(array, 0, array.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<T> AsReadOnlySpan<T>(this NativeArray<T> array) where T : unmanaged
		{
			return AsReadOnlySpan(array, 0, array.Length);
		}

		// TODO: Exception on un-matching length
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<T2> Reinterpret<T1, T2>(this ReadOnlySpan<T1> items)
			where T2 : unmanaged where T1 : unmanaged
		{
			Debug.Assert(UnsafeUtility.SizeOf<T1>() == UnsafeUtility.SizeOf<T2>());
			return MemoryMarshal.Cast<T1, T2>(items);
		}

		// TODO: Exception on un-matching length
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<TTo> Reinterpret<TFrom, TTo>(this Span<TFrom> items)
			where TTo : unmanaged where TFrom : unmanaged
		{
			Debug.Assert(UnsafeUtility.SizeOf<TFrom>() == UnsafeUtility.SizeOf<TTo>());
			return MemoryMarshal.Cast<TFrom, TTo>(items);
		}

		// TODO: Exception on un-matching length
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref TTo Reinterpret<TFrom, TTo>(ref TFrom from) where TFrom : unmanaged where TTo : unmanaged
		{
			Debug.Assert(UnsafeUtility.SizeOf<TFrom>() == UnsafeUtility.SizeOf<TTo>());
			return ref Unsafe.As<TFrom, TTo>(ref from);
		}
	}
}
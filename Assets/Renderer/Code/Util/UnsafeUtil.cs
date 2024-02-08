using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Renderer
{
	public static unsafe class UnsafeUtil
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T* Malloc<T>(Allocator allocator) where T : unmanaged
		{
			var size = UnsafeUtility.SizeOf<T>();
			var align = UnsafeUtility.AlignOf<T>();
			var ptr = (T*)UnsafeUtility.Malloc(size, align, allocator);

			return ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T* MallocPersistentInitialized<T>() where T : unmanaged
		{
			var ptr = Malloc<T>(Allocator.Persistent);
			*ptr = default;

			return ptr;
		}
	}
}
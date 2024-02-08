using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Renderer
{
	public static unsafe class UnsafeUtil
	{
		public static T* Malloc<T>(Allocator allocator) where T : unmanaged
		{
			var size = UnsafeUtility.SizeOf<T>();
			var align = UnsafeUtility.AlignOf<T>();
			var ptr = (T*)UnsafeUtility.Malloc(size, align, allocator);

			return ptr;
		}

		public static T* MallocPersistent<T>() where T : unmanaged
		{
			return Malloc<T>(Allocator.Persistent);
		}
	}
}
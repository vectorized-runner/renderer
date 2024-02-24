using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Renderer
{
	public unsafe struct UnsafeMemory<T> where T : unmanaged
	{
		public T* Ptr;
		public Allocator Allocator;

		public static UnsafeMemory<T> Alloc(Allocator allocator)
		{
			var size = UnsafeUtility.SizeOf<T>();
			var align = UnsafeUtility.AlignOf<T>();
			var ptr = (T*)UnsafeUtility.Malloc(size, align, allocator);
			*ptr = default;

			return new UnsafeMemory<T>
			{
				Ptr = ptr,
				Allocator = allocator,
			};
		}

		public void Dispose()
		{
			UnsafeUtility.Free(Ptr, Allocator);
		}
	}
}
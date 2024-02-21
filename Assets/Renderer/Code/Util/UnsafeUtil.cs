using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Renderer
{
	public static unsafe class UnsafeUtil
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DisposeIfCreated<T>(this NativeArray<T> array) where T : unmanaged
		{
			if (array.IsCreated)
			{
				array.Dispose();
			}
		}
	}
}
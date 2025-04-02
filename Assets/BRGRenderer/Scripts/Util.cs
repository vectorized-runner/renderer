using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BRGRenderer
{
    public static unsafe class Util
    {
        public static T* Malloc<T>(int count, Allocator allocator) where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * count, UnsafeUtility.AlignOf<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAsRef<T>(this NativeArray<T> arr, int index) where T : unmanaged
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(arr.GetUnsafePtr(), index);
        }
    }
}
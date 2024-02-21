using Unity.Profiling;

namespace Renderer
{
	public readonly ref struct AutoProfilerMarker
	{
		public readonly ProfilerMarker Marker;

		public AutoProfilerMarker(string label)
		{
			Marker = new ProfilerMarker(label);
			Marker.Begin();
		}

		public void Dispose()
		{
			Marker.End();
		}
	}
}
namespace Renderer
{
	public static class RenderConstants
	{
		// TODO: Currently this count is low because we're initializing array per RenderMesh, even if it's not used.
		// Fix this inefficiency as we want to support as much as Meshes possible.
		public const int MaxSupportedUniqueMeshCount = ushort.MaxValue;
	}
}
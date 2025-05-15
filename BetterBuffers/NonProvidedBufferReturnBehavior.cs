namespace BetterBuffers;

/// <summary>
/// Various ways to handle returning buffers that did not come from this buffer pool
/// </summary>
public enum NonProvidedBufferReturnBehavior
{
	AddToPool,
	Ignore,
	DebugFailAndAddToPool,
	DebugFailAndIgnore,
	ThrowException,
}
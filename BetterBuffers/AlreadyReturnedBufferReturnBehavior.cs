namespace BetterBuffers;

/// <summary>
/// Various ways to handle returning buffers to the pool that were already returned and marked "available"
/// </summary>
public enum AlreadyReturnedBufferReturnBehavior
{
	Ignore,
	DebugFail,
	ThrowException,
}
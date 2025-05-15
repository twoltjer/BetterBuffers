namespace BetterBuffers;

/// <summary>
/// Various ways to handle returning buffers to the pool that were already returned and marked "available"
/// </summary>
public enum AlreadyReturnedBufferReturnBehavior
{
	/// <summary>
	/// Ignore/don't check if a buffer has already been previously returned. Best performance, but can lead to unexpected problems if an issue exists in user code that violates expected behavior.
	/// </summary>
	Ignore,
	/// <summary>
	/// Checks if a buffer has been previously returned and is waiting to be rented, and triggers a Debug.Fail() if it has been.
	/// </summary>
	DebugFail,
	/// <summary>
	/// Checks if a buffer has been previously returned and is waiting to be rented, and throws an exception if it has been.
	/// </summary>
	ThrowException,
}
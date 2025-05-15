namespace BetterBuffers;

/// <summary>
/// Various ways to handle returning buffers that did not come from this buffer pool
/// </summary>
public enum NonProvidedBufferReturnBehavior
{
	/// <summary>
	/// The pool adopts the buffer as if it was from the pool. This is more dangerous, but fast. 
	/// </summary>
	AddToPool,
	/// <summary>
	/// The buffer is ignored and not returned to the pool.
	/// </summary>
	Ignore,
	/// <summary>
	/// Raise a Debug.Fail() message, but add the buffer to the pool.
	/// </summary>
	DebugFailAndAddToPool,
	/// <summary>
	/// Raise a Debug.Fail() message, and ignore the buffer (don't add it to the available pool). This is the safest option.
	/// </summary>
	DebugFailAndIgnore,
	/// <summary>
	/// Throw an exception for user code to handle.
	/// </summary>
	ThrowException,
}
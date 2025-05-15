using System;
using System.Diagnostics.CodeAnalysis;

namespace BetterBuffers;

/// <summary>
/// A thread-safe high-performance memory buffer pool
/// </summary>
public interface IBufferPool<T> where T : unmanaged
{
	/// <summary>
	/// Rents an array of exact length. A little less performant compared to <see cref="RentMemory"/> or <see cref="Rent"/>, but likely easier to integrate with existing (heap allocated) array allocations.
	/// </summary>
	/// <param name="length">The length of the array to rent</param>
	/// <param name="initializeWithDefaultValues">Initialize the array with default(T). True requires a little extra time. False can have past buffer values in the returned buffer.</param>
	/// <exception cref="OutOfMemoryException">Thrown when the request requires a heap allocation but the system cannot allocate the requested size</exception>
	/// <exception cref="OverflowException">Thrown when the total byte size of the array exceeds <see cref="int.MaxValue"/> or system limits</exception>
	/// <exception cref="ArgumentOutOfRangeException">When a negative size is specified</exception>
	/// <exception cref="TypeLoadException">When T is a type that can't be loaded</exception>
	/// <exception cref="NotSupportedException">When T is a type that cannot be used for an array (e.g. pointers)</exception>
	/// <returns>A T[] with the given length</returns>
	T[] RentExactly(int length, bool initializeWithDefaultValues);

	/// <summary>
	/// Rents a <see cref="Memory{T}"/> of exact length.
	/// </summary>
	/// <param name="length">The length of the buffer to rent</param>
	/// <param name="initializeWithDefaultValues">Initialize the buffer with default(T). True requires a little extra time. False can have past buffer values in the returned buffer.</param>
	/// <exception cref="OutOfMemoryException">Thrown when the request requires a heap allocation but the system cannot allocate the requested size</exception>
	/// <exception cref="OverflowException">Thrown when the total byte size of the underlying array exceeds <see cref="int.MaxValue"/> or system limits</exception>
	/// <exception cref="ArgumentOutOfRangeException">When a negative size is specified</exception>
	/// <exception cref="TypeLoadException">When T is a type that can't be loaded</exception>
	/// <exception cref="NotSupportedException">When T is a type that cannot be used for an array (e.g. pointers)</exception>
	/// <returns>A <see cref="Memory{T}"/> with the given length</returns>
	Memory<T> RentMemory(int length, bool initializeWithDefaultValues);

	/// <summary>
	/// Rents an array with a given length or larger. Similar to ArrayPool.Rent().
	/// </summary>
	/// <param name="minimumLength">The minimum length of the array to rent</param>
	/// <param name="initializeWithDefaultValues">Initialize the array with default(T). True requires a little extra time. False can have past buffer values in the returned buffer.</param>
	/// <exception cref="OutOfMemoryException">Thrown when the request requires a heap allocation but the system cannot allocate the requested size</exception>
	/// <exception cref="OverflowException">Thrown when the total byte size of the array exceeds <see cref="int.MaxValue"/> or system limits</exception>
	/// <exception cref="ArgumentOutOfRangeException">When a negative size is specified</exception>
	/// <exception cref="TypeLoadException">When T is a type that can't be loaded</exception>
	/// <exception cref="NotSupportedException">When T is a type that cannot be used for an array (e.g. pointers)</exception>
	/// <returns>A T[] with at least the given length</returns>
	T[] Rent(int minimumLength, bool initializeWithDefaultValues);

	/// <summary>
	/// Attempts to rent an array of exact length. A little less performant compared to <see cref="TryRentMemory"/> or <see cref="Rent"/>, but likely easier to integrate with existing (heap allocated) array allocations.
	/// </summary>
	/// <param name="length">The length of the array to rent</param>
	/// <param name="initializeWithDefaultValues">Initialize the array with default(T). True requires a little extra time. False can have past buffer values in the returned buffer.</param>
	/// <param name="array">The array if it could be provided, null otherwise.</param>
	/// <returns>True if an array could be rented, false otherwise.</returns>
	bool TryRentExactly(int length, bool initializeWithDefaultValues, [NotNullWhen(true)] out T[]? array);

	/// <summary>
	/// Attempts to rent buffer of exact size.
	/// </summary>
	/// <param name="length">The minimum length of the array to rent</param>
	/// <param name="initializeWithDefaultValues">Initialize the buffer with default(T). True requires a little extra time. False can have past buffer values in the returned buffer.</param>
	/// <param name="buffer">The buffer if it could be provided, null otherwise.</param>
	/// <returns>True if a buffer could rented, false otherwise.</returns>
	bool TryRentMemory(int length, bool initializeWithDefaultValues, [NotNullWhen(true)] out Memory<T>? buffer);

	/// <summary>
	/// Attempts to rent an array of at least a specified length.
	/// </summary>
	/// <param name="minimumLength">The minimum length of the array to rent</param>
	/// <param name="initializeWithDefaultValues">Initialize the array with default(T). True requires a little extra time. False can have past buffer values in the returned buffer.</param>
	/// <param name="array">The array if it could be provided, null otherwise.</param>
	/// <returns>True if an array could rented, false otherwise.</returns>
	bool TryRent(int minimumLength, bool initializeWithDefaultValues, [NotNullWhen(true)] out T[]? array);

	/// <summary>
	/// Returns an array to the pool.
	/// </summary>
	/// <param name="array">The array to return to the pool</param>
	/// <param name="nonProvidedBufferReturnBehavior">How to handle an array that did not originate from this pool</param>
	/// <param name="alreadyReturnedBufferReturnBehavior">How to handle an array that was already returned to this pool and marked available</param>
	void Return(
		T[] array,
		NonProvidedBufferReturnBehavior nonProvidedBufferReturnBehavior = NonProvidedBufferReturnBehavior.Ignore,
		AlreadyReturnedBufferReturnBehavior alreadyReturnedBufferReturnBehavior =
			AlreadyReturnedBufferReturnBehavior.Ignore
		);


	/// <summary>
	/// Returns a memory buffer to the pool.
	/// </summary>
	/// <param name="memory">The buffer to return to the pool</param>
	/// <param name="nonProvidedBufferReturnBehavior">How to handle a memory buffer that did not originate from this pool</param>
	/// <param name="alreadyReturnedBufferReturnBehavior">How to handle a memory buffer that was already returned to this pool and marked available</param>
	void Return(
		Memory<T> memory,
		NonProvidedBufferReturnBehavior nonProvidedBufferReturnBehavior = NonProvidedBufferReturnBehavior.Ignore,
		AlreadyReturnedBufferReturnBehavior alreadyReturnedBufferReturnBehavior =
			AlreadyReturnedBufferReturnBehavior.Ignore
		);

	/// <summary>
	/// Get the total number of bytes out "in the wild" via unreturned rentals.
	/// </summary>
	long GetTotalUsedBytes();

	/// <summary>
	/// Gets the total number of bytes associated with the buffers allocated by this pool
	/// </summary>
	long GetTotalAllocatedBytes();

	/// <summary>
	/// Gets the total number of bytes that are currently available for non-allocating rentals.
	/// </summary>
	long GetTotalAvailableBytes();
}
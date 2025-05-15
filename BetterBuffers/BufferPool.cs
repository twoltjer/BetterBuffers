using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace BetterBuffers;

public class BufferPool<T> : IBufferPool<T>, IDisposable, IAsyncDisposable where T : unmanaged
{
	private readonly ConcurrentDictionary<int, BufferState<T>> _bufferStates =
		new ConcurrentDictionary<int, BufferState<T>>();

	private readonly Timer _timer;

	private unsafe int GetMaxValues() => (1 << 30) / sizeof(T);

	public BufferPool()
	{
		_timer = new Timer(OnTimerTick, this, TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5));
	}

	private static void OnTimerTick(object? state)
	{
		if (state is not BufferPool<T> bufferPool)
		{
			Debug.Fail("Expected BufferPool state");
			return;
		}

		bufferPool.OnTimerTick();
	}

	private void OnTimerTick()
	{
		foreach (var bufferState in _bufferStates.Values)
			bufferState.ProcessPendingWork();
	}

	public T[] RentExactly(int length, bool initializeWithDefaultValues)
	{
		if (length < 0)
			throw new ArgumentOutOfRangeException(nameof(length));

		if (length == 0)
			return Array.Empty<T>();

		var maxLength = GetMaxValues();
		if (length > maxLength)
			throw new OverflowException($"Maximum length that can be rented is {maxLength}");

		var array = RentInternal(length);
		if (initializeWithDefaultValues)
			array.AsSpan().Fill(default);
		return array;
	}

	private T[] RentInternal(int length)
	{
		var bufferState = _bufferStates.GetOrAdd(length, new BufferState<T>(length));
		var buffer = bufferState.GetBuffer();
		return buffer;
	}

	public Memory<T> RentMemory(int length, bool initializeWithDefaultValues)
	{
		if (length < 0)
			throw new ArgumentOutOfRangeException(nameof(length));

		if (length == 0)
			return Memory<T>.Empty;

		var maxLength = GetMaxValues();
		if (length > maxLength)
			throw new OverflowException($"Maximum length that can be rented is {maxLength}");

		var internalLength = NextPowerOfTwo(length);
		var array = RentInternal(internalLength);
		var memory = array.AsMemory().Slice(0, length);

		if (initializeWithDefaultValues)
			memory.Span.Fill(default);

		return memory;
	}

	private static int NextPowerOfTwo(int value)
	{
		// Power of two
		if ((value & (value - 1)) == 0)
			return value;

		// Not power of two
		value |= value >> 1;
		value |= value >> 2;
		value |= value >> 4;
		value |= value >> 8;
		value |= value >> 16;

		return value;
	}

	public T[] Rent(int minimumLength, bool initializeWithDefaultValues)
	{
		if (minimumLength < 0)
			throw new ArgumentOutOfRangeException(nameof(minimumLength));

		if (minimumLength == 0)
			return Array.Empty<T>();

		var maxLength = GetMaxValues();
		if (minimumLength > maxLength)
			throw new OverflowException($"Maximum length that can be rented is {maxLength}");

		var internalLength = NextPowerOfTwo(minimumLength);
		var array = RentInternal(internalLength);

		if (initializeWithDefaultValues)
			array.AsSpan().Fill(default);

		return array;
	}

	public bool TryRentExactly(int length, bool initializeWithDefaultValues, [NotNullWhen(true)] out T[]? array)
	{
		try
		{
			array = RentExactly(length, initializeWithDefaultValues);
			return true;
		}
		catch (Exception)
		{
			array = null;
			return false;
		}
	}

	public bool TryRentMemory(int length, bool initializeWithDefaultValues, [NotNullWhen(true)] out Memory<T>? buffer)
	{
		try
		{
			buffer = RentMemory(length, initializeWithDefaultValues);
			return true;
		}
		catch (Exception)
		{
			buffer = null;
			return false;
		}
	}

	public bool TryRent(int minimumLength, bool initializeWithDefaultValues, [NotNullWhen(true)] out T[]? array)
	{
		try
		{
			array = Rent(minimumLength, initializeWithDefaultValues);
			return true;
		}
		catch (Exception)
		{
			array = null;
			return false;
		}
	}

	public void Return(
		T[] array,
		NonProvidedBufferReturnBehavior nonProvidedBufferReturnBehavior = NonProvidedBufferReturnBehavior.Ignore,
		AlreadyReturnedBufferReturnBehavior alreadyReturnedBufferReturnBehavior =
			AlreadyReturnedBufferReturnBehavior.Ignore
		)
	{
		var length = array.Length;
		if (!_bufferStates.TryGetValue(length, out var bufferState))
		{
			switch (nonProvidedBufferReturnBehavior)
			{
				case NonProvidedBufferReturnBehavior.AddToPool:
					bufferState = _bufferStates.GetOrAdd(length, new BufferState<T>(length));
					break;
				case NonProvidedBufferReturnBehavior.Ignore:
					return;
				case NonProvidedBufferReturnBehavior.DebugFailAndAddToPool:
					Debug.Fail("Returning buffer to pool it did not originate from");
					bufferState = _bufferStates.GetOrAdd(length, new BufferState<T>(length));
					break;
				case NonProvidedBufferReturnBehavior.DebugFailAndIgnore:
					Debug.Fail("Returning buffer to pool it did not originate from");
					return;
				case NonProvidedBufferReturnBehavior.ThrowException:
					throw new ArgumentException("Returning buffer to pool it did not originate from");
				default:
					throw new ArgumentOutOfRangeException(
						nameof(nonProvidedBufferReturnBehavior),
						nonProvidedBufferReturnBehavior,
						null
						);
			}
		}

		bufferState.ReturnBuffer(array, nonProvidedBufferReturnBehavior, alreadyReturnedBufferReturnBehavior);
	}

	public void Return(
		Memory<T> memory,
		NonProvidedBufferReturnBehavior nonProvidedBufferReturnBehavior = NonProvidedBufferReturnBehavior.AddToPool,
		AlreadyReturnedBufferReturnBehavior alreadyReturnedBufferReturnBehavior =
			AlreadyReturnedBufferReturnBehavior.Ignore
		)
	{
		if (!MemoryMarshal.TryGetArray<T>(memory, out var arraySegment) || arraySegment.Array is not { } array)
		{
			// Can't even resolve array, so can't be added to pool even if that behavior was specified
			switch (nonProvidedBufferReturnBehavior)
			{
				case NonProvidedBufferReturnBehavior.AddToPool:
				case NonProvidedBufferReturnBehavior.Ignore:
					return;
				case NonProvidedBufferReturnBehavior.DebugFailAndAddToPool:
				case NonProvidedBufferReturnBehavior.DebugFailAndIgnore:
					Debug.Fail("Returning memory that did not originate in this pool");
					return;
				case NonProvidedBufferReturnBehavior.ThrowException:
					throw new ArgumentException("Returning memory that did not originate in this pool");
				default:
					throw new ArgumentOutOfRangeException(
						nameof(nonProvidedBufferReturnBehavior),
						nonProvidedBufferReturnBehavior,
						null
						);
			}
		}

		Return(array, nonProvidedBufferReturnBehavior, alreadyReturnedBufferReturnBehavior);
	}

	public long GetTotalUsedBytes()
	{
		long totalUsedBytes = 0;
		foreach (var bufferState in _bufferStates.Values)
		{
			var bufferStateUsedBytes = bufferState.GetUsedBytes();
			totalUsedBytes += bufferStateUsedBytes;
		}

		return totalUsedBytes;
	}

	public long GetTotalAllocatedBytes()
	{
		return GetTotalUsedBytes() + GetTotalAvailableBytes();
	}

	public long GetTotalAvailableBytes()
	{
		long totalAvailableBytes = 0;
		foreach (var bufferState in _bufferStates.Values)
		{
			var bufferStateAvailableBytes = bufferState.GetAvailableBytes();
			totalAvailableBytes += bufferStateAvailableBytes;
		}

		return totalAvailableBytes;
	}

	public void Dispose()
	{
		_timer.Dispose();
	}

	public async ValueTask DisposeAsync()
	{
		await _timer.DisposeAsync();
	}
}
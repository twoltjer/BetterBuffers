using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace BetterBuffers;

internal class BufferState<T> where T : unmanaged
{
	private readonly int _length;
	private long _currentBin = 0;
	private long _initializedBins = 0;
	private const int TrackedIterationsCount = 10;
	private readonly long[] _maxUsedBufferCounts = new long[TrackedIterationsCount];
	private readonly ConcurrentBag<T[]> _rentedBuffers = new ConcurrentBag<T[]>();
	private readonly ConcurrentBag<T[]> _availableBuffers = new ConcurrentBag<T[]>();

	public BufferState(int length)
	{
		_length = length;
	}

	public void ProcessPendingWork()
	{
		_initializedBins = Math.Max(_currentBin + 1, _initializedBins);
		var averageMaxUsedBuffers = 0d;
		for (var index = 0; index < _initializedBins; index++)
		{
			var value = _maxUsedBufferCounts[index];
			averageMaxUsedBuffers += value;
		}

		averageMaxUsedBuffers /= _initializedBins;
		// Increase by 10% just for a little wiggle room/comfort
		var targetBufferCount = averageMaxUsedBuffers * 1.1;
		var currentBufferCount = _rentedBuffers.Count + _availableBuffers.Count;
		for (int i = currentBufferCount; i > targetBufferCount; i--)
		{
			if (!_availableBuffers.TryTake(out _))
				break;
		}

		_currentBin = (_currentBin + 1) % TrackedIterationsCount;
		_maxUsedBufferCounts[_currentBin] = 0;
	}

	public T[] GetBuffer()
	{
		if (!_availableBuffers.TryTake(out var buffer))
		{
			buffer = new T[_length];
		}

		_rentedBuffers.Add(buffer);

		long usedBufferCount = _rentedBuffers.Count;
		var currentBin = Interlocked.Read(ref _currentBin);
		while (Interlocked.Read(ref _maxUsedBufferCounts[currentBin]) < usedBufferCount)
		{
			var removedValue = Interlocked.Exchange(ref _maxUsedBufferCounts[currentBin], usedBufferCount);
			// Check if someone else put in an even higher value. If so, we want to put it back in and not replace it with our read of _rentedBuffers.Count
			if (removedValue > usedBufferCount)
				usedBufferCount = removedValue;
		}

		return buffer;
	}

	private bool ContainsArray(ConcurrentBag<T[]> bag, T[] array)
	{
		foreach (var item in bag)
		{
			if (item == array) return true;
		}

		return false;
	}

	public void ReturnBuffer(
		T[] array,
		NonProvidedBufferReturnBehavior nonProvidedBufferReturnBehavior,
		AlreadyReturnedBufferReturnBehavior alreadyReturnedBufferReturnBehavior
		)
	{
		CheckAlreadyReturned(array, alreadyReturnedBufferReturnBehavior);
		if (CheckInRentedBuffersCausesIgnore(array, nonProvidedBufferReturnBehavior))
			return;
		_availableBuffers.Add(array);
	}

	private void CheckAlreadyReturned(
		T[] array,
		AlreadyReturnedBufferReturnBehavior alreadyReturnedBufferReturnBehavior
		)
	{
		if (alreadyReturnedBufferReturnBehavior != AlreadyReturnedBufferReturnBehavior.Ignore)
		{
			if (ContainsArray(_availableBuffers, array))
			{
				if (alreadyReturnedBufferReturnBehavior == AlreadyReturnedBufferReturnBehavior.DebugFail)
				{
					Debug.Fail("Returning buffer that already is available");
				}
				else
				{
					Debug.Assert(
						alreadyReturnedBufferReturnBehavior == AlreadyReturnedBufferReturnBehavior.ThrowException
						);
					throw new InvalidOperationException("Returning buffer that already is available");
				}
			}
		}
	}

	private bool CheckInRentedBuffersCausesIgnore(
		T[] array,
		NonProvidedBufferReturnBehavior nonProvidedBufferReturnBehavior
		)
	{
		if (nonProvidedBufferReturnBehavior == NonProvidedBufferReturnBehavior.AddToPool) 
			return false;
		if (ContainsArray(_rentedBuffers, array)) 
			return false;
		switch (nonProvidedBufferReturnBehavior)
		{
			case NonProvidedBufferReturnBehavior.Ignore:
				return true;
			case NonProvidedBufferReturnBehavior.DebugFailAndAddToPool:
				Debug.Fail("Returning buffer that didn't originate from this pool");
				return false;
			case NonProvidedBufferReturnBehavior.DebugFailAndIgnore:
				Debug.Fail("Returning buffer that didn't originate from this pool");
				return true;
			case NonProvidedBufferReturnBehavior.ThrowException:
				throw new InvalidOperationException("Returning buffer that didn't originate from this pool");
			default:
				throw new ArgumentOutOfRangeException(
					nameof(nonProvidedBufferReturnBehavior),
					nonProvidedBufferReturnBehavior,
					null
					);
		}
	}

	public unsafe long GetAvailableBytes()
	{
		return (long)sizeof(T) * _availableBuffers.Count * _length;
	}

	public unsafe long GetUsedBytes()
	{
		return (long)sizeof(T) * _rentedBuffers.Count * _length;
	}
}
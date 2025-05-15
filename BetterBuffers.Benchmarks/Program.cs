using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BetterBuffers;

BenchmarkRunner.Run<BufferPoolBenchmark>();

/// <summary>
/// Creates a 
/// </summary>
[MemoryDiagnoser]
public class BufferPoolBenchmark
{
	private BufferPool<byte> _pool = null!;
	private int[] _sizes = null!;
	private Queue<Memory<byte>> _memoryQueue = null!;
	private Random _random = null!;
	private ArrayPool<byte> _arrayPool = null!;
	private const double Lies = 0.53;
	private const int TargetMemoryQueue = 4800;

	[GlobalSetup]
	public void Setup()
	{
		_pool = new BufferPool<byte>();
		_random = new Random(0);
		_sizes = new int[3_000_000];
		const int sizeLimit = 10 * 1024 * 1024;
		for (int i = 0; i < _sizes.Length; i++)
		{
			var size = (int)Math.Round(_random.NextDouble() * _random.NextDouble() * sizeLimit);
			if (size == 0)
			{
				i--;
				continue;
			}
			_sizes[i] = size;
		}

		_memoryQueue = new Queue<Memory<byte>>(capacity: _sizes.Length);
		_arrayPool = ArrayPool<byte>.Create(maxArrayLength: sizeLimit, maxArraysPerBucket: 50);
		GC.Collect();
	}

	[Benchmark]
	public void UsingHeap_Short()
	{
		for (int i = 0; i < 50_000; i++)
		{
			var buffer = new byte[_sizes[i]];
			_memoryQueue.Enqueue(buffer);
			while (true)
			{
				var lies = _random.NextDouble() > Lies;
				var remove = _memoryQueue.Count > TargetMemoryQueue;
				if (lies)
					remove = !remove;
				if (remove)
				{
					if (_memoryQueue.TryDequeue(out _))
					{
					}
				}
				else
				{
					break;
				}
			}
		}
	}
	
	[Benchmark]
	public void UsingArrayPool_Long()
	{
		_memoryQueue.Clear();
		var arrayQueue = new Queue<byte[]>();
		for (int i = 0; i < 3_000_000; i++)
		{
			var buffer = _arrayPool.Rent(_sizes[i]);
			var memory = buffer.AsMemory().Slice(0, _sizes[i]);
			_memoryQueue.Enqueue(memory);
			arrayQueue.Enqueue(buffer);
			while (true)
			{
				var lies = _random.NextDouble() > Lies;
				var remove = _memoryQueue.Count > TargetMemoryQueue;
				if (lies)
					remove = !remove;
				if (remove)
				{
					if (_memoryQueue.TryDequeue(out _))
					{
						_arrayPool.Return(arrayQueue.Dequeue());
					}
				}
				else
				{
					break;
				}
			}
		}
	}
	
	[Benchmark]
	public void UsingBetterBuffers_Long()
	{
		for (int i = 0; i < 3_000_000; i++)
		{
			var buffer = _pool.RentMemory(_sizes[i], false);
			_memoryQueue.Enqueue(buffer);
			while (true)
			{
				var lies = _random.NextDouble() > Lies;
				var remove = _memoryQueue.Count > TargetMemoryQueue;
				if (lies)
					remove = !remove;
				if (remove)
				{
					if (_memoryQueue.TryDequeue(out var rtnBuffer))
					{
						_pool.Return(rtnBuffer);
					}
				}
				else
				{
					break;
				}
			}
		}
	}
	
	[Benchmark]
	public void UsingArrayPool_Short()
	{
		_memoryQueue.Clear();
		var arrayQueue = new Queue<byte[]>();
		for (int i = 0; i < 50_000; i++)
		{
			var buffer = _arrayPool.Rent(_sizes[i]);
			var memory = buffer.AsMemory().Slice(0, _sizes[i]);
			_memoryQueue.Enqueue(memory);
			arrayQueue.Enqueue(buffer);
			while (true)
			{
				var lies = _random.NextDouble() > Lies;
				var remove = _memoryQueue.Count > TargetMemoryQueue;
				if (lies)
					remove = !remove;
				if (remove)
				{
					if (_memoryQueue.TryDequeue(out _))
					{
						_arrayPool.Return(arrayQueue.Dequeue());
					}
				}
				else
				{
					break;
				}
			}
		}
	}
	
	[Benchmark]
	public void UsingBetterBuffers_Short()
	{
		for (int i = 0; i < 50_000; i++)
		{
			var buffer = _pool.RentMemory(_sizes[i], false);
			_memoryQueue.Enqueue(buffer);
			while (true)
			{
				var lies = _random.NextDouble() > Lies;
				var remove = _memoryQueue.Count > TargetMemoryQueue;
				if (lies)
					remove = !remove;
				if (remove)
				{
					if (_memoryQueue.TryDequeue(out var rtnBuffer))
					{
						_pool.Return(rtnBuffer);
					}
				}
				else
				{
					break;
				}
			}
		}
	}
}
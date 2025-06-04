# Tutorial 01: Async data buffer producer/consumer 

In this tutorial, we will use the BetterBuffers library to drastically improve an asynchronous data producer/consumer program. 

## Setup

The below code is a very simple implementation of a producer and consumer of data, which can run simultaneously. 

```csharp
using System.Collections.Concurrent;

const int chunkCount = 1024;

// Must be a ConcurrentQueue or restricted by a critical section because we can enqueue and dequeue simultaneously
var dataQueue = new ConcurrentQueue<byte[]>();
var semaphore = new SemaphoreSlim(0);

await Task.WhenAll(Task.Run(ProduceData), Task.Run(ConsumeData));
return;

async Task ProduceData()
{
	var random = new Random(Seed: 0);
	for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
	{
		// Use Task.Delay to simulate an expensive data production task that can take anywhere between 0 and 12 ms
		await Task.Delay(random.Next() % 12);
		var chunkSize = random.Next() % 8192 + 1;
		var chunkData = new byte[chunkSize];
		random.NextBytes(chunkData);
		dataQueue.Enqueue(chunkData);
		semaphore.Release();
	}
}

async Task ConsumeData()
{
	var random = new Random(Seed: 1);
	for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
	{
		await semaphore.WaitAsync();
        var data = dataQueue.TryDequeue(out var d) ? d : throw new InvalidOperationException();
		// Use Task.Delay() to simulate an expensive data consumption task that can take anywhere between 0 and 15 ms
		await Task.Delay(random.Next() % 15);
	}
}
```

Running this code under a benchmarking or profiling tool, one can see that it has several megabytes of heap allocations for the `chunkData` arrays. This can be easily improved.

## Basic heap allocation substitute

First, use a BetterBuffers `BufferPool<byte>()` to serve the allocated arrays. Add a pool instance to shared scope:

```csharp
var dataQueue = new Queue<byte>();
var semaphore = new SemaphoreSlim(0);
await using var bufferPool = new BufferPool<byte>(); // from the BetterBuffers namespace

await Task.WhenAll(Task.Run(ProduceData), Task.Run(ConsumeData));
```

Then, modify the producer to fetch an array from this pool, instead of creating a new one on the heap every time:

```csharp
var chunkSize = random.Next() % 8192 + 1;
// May allocate, or may already have a correctly sized array available and reduce allocations.
// Note that since we're overwriting the entire array's contents, we don't need to initialize them when fetching the array.
var chunkData = bufferPool.RentExactly(chunkSize, initializeWithDefaultValues: false);
random.NextBytes(chunkData);
```

Correspondingly, the consumer should return the array after it has been used:

```csharp
async Task ConsumeData()
{
	var random = new Random(Seed: 1);
	for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
	{
		await semaphore.WaitAsync();
		var data = dataQueue.TryDequeue(out var d) ? d : throw new InvalidOperationException();
		// Use Task.Delay() to simulate an expensive data consumption task that can take anywhere between 0 and 15 ms
		await Task.Delay(random.Next() % 15);
		bufferPool.Return(data);
	}
}
```

## Even more efficiency using Memory\<T\>

The heap allocations can be further reduced by utilizing `BufferPool<T>.RentMemory()` method. This requires changing the queue from a `ConcurrentQueue<byte[]>` to a `ConcurrentQueue<Memory<byte>>`:

```csharp
const int chunkCount = 1024;

// Must be a ConcurrentQueue or restricted by a critical section because we can enqueue and dequeue simultaneously
var dataQueue = new ConcurrentQueue<Memory<byte>>();
var semaphore = new SemaphoreSlim(0);
await using var bufferPool = new BufferPool<byte>();
```

Additionally, the producer should call `RentMemory()` instead of `RentExactly()`:

```csharp
var chunkSize = random.Next() % 8192 + 1;
var chunkData = bufferPool.RentMemory(chunkSize, initializeWithDefaultValues: false);
random.NextBytes(chunkData.Span); // Use Memory<T>.Span when filling from Random 
dataQueue.Enqueue(chunkData);
```

Because the consumer task doesn't mind having a `Memory<byte>` for its data, it doesn't need to change at all. The correct `Return()` overload is automatically used to return the rented memory to `bufferPool`.

Using this approach, the heap memory usage has been reduced to less than half of what the original code required.
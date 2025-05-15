namespace BetterBuffers.Tests;

public class BufferStateTests
{
	private const int TestsThreadCount = 128;
	
	[Fact]
	public async Task TestRentingMultipleThreadsWithNoAvailable()
	{
		var bufferState = new BufferState<int>(16);
		// Repeat a delegate that creates a task, so that we don't end up with the same task instance repeated
		var tasks = Enumerable.Repeat(() => Task.Run(bufferState.GetBuffer), TestsThreadCount).Select(taskDel => taskDel.Invoke()).ToArray();
		var results = await Task.WhenAll(tasks);
		Assert.Equal(TestsThreadCount, results.Distinct().Count());
		foreach (var result in results)
			Assert.Equal(16, result.Length);
	}

	[Fact]
	public async Task TestRentingMultipleThreadsWithSomeAvailable()
	{
		var bufferState = new BufferState<int>(16);
		// Prepare bufferState to have TestsThreadCount / 2 buffers available already;
		var renteds = Enumerable.Repeat(bufferState.GetBuffer, TestsThreadCount / 2).Select(del => del.Invoke()).ToArray();
		foreach (var rented in renteds)
			bufferState.ReturnBuffer(rented, NonProvidedBufferReturnBehavior.ThrowException, AlreadyReturnedBufferReturnBehavior.ThrowException);
		var tasks = Enumerable.Repeat(() => Task.Run(bufferState.GetBuffer), TestsThreadCount).Select(taskDel => taskDel.Invoke()).ToArray();
		var results = await Task.WhenAll(tasks);
		Assert.Equal(TestsThreadCount, results.Distinct().Count());
		foreach (var result in results)
			Assert.Equal(16, result.Length);
	}
}
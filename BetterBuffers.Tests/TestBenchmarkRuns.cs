namespace BetterBuffers.Tests;

public class TestBenchmarkRuns
{
	[Fact]
	public void TestUsingArrayPool()
	{
		var instance = new BufferPoolBenchmark();
		instance.UsingArrayPool_Short();
	}

	[Fact]
	public void TestUsingArrayPoolLong()
	{
		var instance = new BufferPoolBenchmark();
		instance.UsingArrayPool_Long();
	}
	
	[Fact]
	public void TestUsingHeap()
	{
		var instance = new BufferPoolBenchmark();
		instance.UsingHeap_Short();
	}

	[Fact]
	public void TestUsingBetterBuffers()
	{
		var instance = new BufferPoolBenchmark();
		instance.UsingBetterBuffers_Short();
	}
	
	[Fact]
	public void TestUsingBetterBuffersLong()
	{
		var instance = new BufferPoolBenchmark();
		instance.UsingBetterBuffers_Long();
	}
}
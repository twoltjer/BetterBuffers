# BetterBuffers

[![Build and Publish](https://github.com/twoltjer/BetterBuffers/actions/workflows/build.yml/badge.svg)](https://github.com/twoltjer/BetterBuffers/actions/workflows/build.yml)

A high-performance replacement for .NET's ArrayPool that delivers similar or better performance with improved memory management.

## Overview

BetterBuffers provides an optimized alternative to the standard .NET ArrayPool, addressing common memory management issues while maintaining or improving performance. It uses advanced algorithms to reduce memory fragmentation, minimize GC pressure, and optimize allocation/deallocation patterns in high-throughput scenarios.

## Features

- **Equivalent or better performance** compared to standard ArrayPool in most scenarios
- **Reduced memory fragmentation** through intelligent buffer sizing and allocation
- **Lower GC pressure** with optimized memory management
- **Thread-safe** implementation suitable for concurrent applications
- **API compatible** with existing ArrayPool implementations for easy migration
- **Customizable** sizing and retention policies

## Benchmarks

```
| Method                   | Mean        | Error      | StdDev     | Median      | Gen0       | Gen1       | Gen2      | Allocated    |
|------------------------- |------------:|-----------:|-----------:|------------:|-----------:|-----------:|----------:|-------------:|
| UsingHeap_Short          | 1,193.13 ms |  38.089 ms | 111.709 ms | 1,186.35 ms |  5000.0000 |  5000.0000 | 4000.0000 | 124702.31 MB |
| UsingArrayPool_Short     | 1,906.81 ms |  63.321 ms | 186.703 ms | 1,906.36 ms |          - |          - |         - |  19434.89 MB |
| UsingBetterBuffers_Short |    26.18 ms |   0.475 ms |   0.397 ms |    26.12 ms |   406.2500 |   406.2500 |         - |     24.72 MB |

| Method                   | Mean        | Error      | StdDev     | Median      | Gen0       | Gen1       | Gen2      | Allocated    |
|------------------------- |------------:|-----------:|-----------:|------------:|-----------:|-----------:|----------:|-------------:|
| UsingArrayPool_Long      | 3,980.47 ms | 128.304 ms | 372.233 ms | 3,853.01 ms |  6000.0000 |  6000.0000 | 6000.0000 | 115406.41 MB |
| UsingBetterBuffers_Long  | 1,625.22 ms |  29.842 ms |  27.914 ms | 1,626.48 ms | 25000.0000 | 12000.0000 |         - |   1220.59 MB |
```

## Installation

```
dotnet add package BetterBuffers
```

## Quick Start

```csharp
using BetterBuffers;

// Create a buffer pool
var pool = new BufferPool<byte>();

// Rent a buffer
byte[] arrayBuffer = pool.RentExactly(1024, initializeWithDefaultValues: true);
// For even better performance, use this:
// Memory<byte> memoryBuffer = pool.RentMemory(1024, initializeWithDefaultValues: false);

try
{
    // Use the buffer...
    ProcessData(buffer);
}
finally
{
    // Return the buffer when done
    pool.Return(buffer);
}
```

## Migration from ArrayPool

BetterBuffers is designed to be a drop-in replacement for ArrayPool:

```csharp
// Before:
using System.Buffers;
var pool = ArrayPool<byte>.Create();

// After:
using BetterBuffers;
var pool = new BufferPool<byte>();
```

## Advanced Usage

### Getting memory segments (improves performance)

Memory<T> is more flexible compared to T[], and can be obtained more efficiently compared to RentExactly()

```csharp
Memory<byte> memoryBuffer = pool.RentMemory(1024, initializeWithDefaultValues: true);
```

### Skipping value initialization (improves performance)

Use if you plan to overwrite the values in the buffer anyway

```csharp
Memory<byte> memoryBuffer = pool.RentMemory(1024, initializeWithDefaultValues: false);
```

### Exceptionless error handling

```csharp
if (!pool.TryRentMemory(1024, initializeWithDefaultValues: true, out Memory<byte> buffer))
{
  Console.WriteLine("Failed to allocate memory");
  return;
}

ProcessData(buffer);
```


## How It Works

BetterBuffers improves on .NET's ArrayPool implementation in several key ways:

1. **Gradually clears buffers that haven't been used in a while, reducing memory usage**
2. **No arbitrary caps on how many buffers can be rented, or number of buckets**
3. **More performant rental logic when a buffer is already available**
4. **Better error handling**
5. **Can provide exact array sizes when needed**

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by .NET's ArrayPool implementation
- Performance testing methodology based on [BenchmarkDotNet](https://benchmarkdotnet.org/)

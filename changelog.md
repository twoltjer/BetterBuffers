# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2025-05-15

### Added
- Initial release of BetterBuffers
- Core BufferPool<T> implementation
- Support for renting and returning arrays and Memory<T>
- Efficient buffer state tracking and management
- Optional initialization of buffer contents
- Configurable behavior for returning non-pool buffers
- Configurable behavior for already-returned buffers
- Memory usage statistics APIs
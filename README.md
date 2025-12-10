# SHISHUA.NET

> [!WARNING]  
> SHISHUA should not be used for cryptographic purposes!

A .NET 8+ reimplementation of [SHISHUA](https://github.com/espadrine/shishua), the 2025 world record holder for the fastest pRNG ever created. It is an incredibly efficient and equally resilient randomizer that is especially useful in cases where a lot of data needs to be generated in bulk.

This port was originally made for [The Conservatory](https://xansworkshop.com/conservatory), an indie game by Xan's Workshop, but was ultimately deemed useful to the general public and was thus released here under the same license as the original algorithm, CC0, for free public use.

## Behavior

SHISHUA.NET is guaranteed to have the same output as the original C++ algorithm. However, it cannot guarantee the same performance profile and characteristics across all hardware. This is because, unlike its C++ counterpart, this makes use of the higher level C# intrinsics API.

The C# intrinsics API abstracts out platform-specific instructions with general representations of their behavior. When JIT compiles the code into native machine code, it will use the best suited algorithm to achieve its goal on the current hardware.

## Supported Variations

Technically, everything the original supports is also supported because of the use of the intrinsics API. If .NET can target it, it probably supports its hardware intrinsics. That aside, both the full-size and half-size implementations are available.
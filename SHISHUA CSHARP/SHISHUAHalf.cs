using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace SHISHUADotNet {

	/// <summary>
	/// An implementation of the <see href="https://github.com/espadrine/shishua">SHISHUA</see> pRNG algorithm, an extremely fast and resilient
	/// pRNG algorithm suitable for generating enormous blocks of data.
	/// <para/>
	/// This is the "half-size" implementation. It is simpler than its full-size counterpart, exporting blocks of 64 bytes instead of the 128,
	/// but in exchange it also runs at half speed.
	/// </summary>
	/// <remarks>
	/// This is not necessarily a drop-in replacement for <see cref="SHISHUA"/> as it will generate different data.
	/// </remarks>
	public class SHISHUAHalf {

		/// <summary>
		/// Generates an amount of bytes using the provided state.
		/// </summary>
		/// <param name="state">The randomizer state.</param>
		/// <param name="resultBuffer">The output buffer to store generated random bytes into. Can be <see langword="null"/> to skip storing data and advance the state anyway.</param>
		/// <param name="generationSize">The amount of bytes to generate. If the <paramref name="resultBuffer"/> is not <see langword="null"/> (or, empty), this must be greater than or equal to its size. Must be divisible by 32.</param>
		/// <exception cref="InvalidOperationException"></exception>
		public static void Generate(ref PrngState state, Span<byte> resultBuffer, int generationSize) {
			if (!resultBuffer.IsEmpty) {
				if (resultBuffer.Length < generationSize) throw new ArgumentException($"The {nameof(generationSize)} parameter must be greater than or equal to {nameof(resultBuffer)}.Length");
				if (generationSize % 32 != 0) throw new ArgumentException($"The {nameof(generationSize)} parameter (and by extension {nameof(resultBuffer)}.Length) must be divisible by 32.");
			}
			Vector256<ulong> o = state.output;
			Vector256<ulong> s0 = state.state0;
			Vector256<ulong> s1 = state.state1;
			Vector256<ulong> counter = state.counter;
			Vector256<ulong> t0 = default;
			Vector256<ulong> t1 = default;
			Vector256<ulong> u0 = default;
			Vector256<ulong> u1 = default;

			Vector256<ulong> increment = Vector256.Create(7UL, 5UL, 3UL, 1UL);

			for (int i = 0; i < generationSize; i += 32) {
				if (!resultBuffer.IsEmpty) {
					unsafe {
						fixed (byte* bufPtr = resultBuffer) {
							o.Store((ulong*)&bufPtr[i]);
						}
					}
				}

				s1 += counter;
				counter += increment;

				u0 = s0 >>> 1;
				u1 = s1 >>> 3;

				// I won't scream at you again, but if you wonder why the operator is created every time here,
				// it has to be. See equivalent code block in the full version of the class for an explanation
				// (tl;dr JIT panics if it's a local and generates abysmally awful code out of caution, instead
				// of emitting a single instruction).
				t0 = Vector256.Shuffle(s0.AsInt32(), Vector256.Create(5, 6, 7, 0, 1, 2, 3, 4)).AsUInt64();
				t1 = Vector256.Shuffle(s1.AsInt32(), Vector256.Create(3, 4, 5, 6, 7, 0, 1, 2)).AsUInt64();

				s0 = t0 + u0;
				s1 = t1 + u1;

				o = Vector256.Xor(u0, t1);
			}
			state.output = o;
			state.state0 = s0;
			state.state1 = s1;
			state.counter = counter;
		}

		/// <summary>
		/// Initializes the randomizer state using AVX2 logic.
		/// </summary>
		/// <param name="seed0">The first 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed1">The second 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed2">The third 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed3">The fourth 64 of 256 bits needed to create a seed.</param>
		public static PrngState Initialize(ulong seed0, ulong seed1, ulong seed2, ulong seed3) {
			const int STEPS = 5;
			const int ROUNDS = 4;

			PrngState state = default;
			/*
			private static readonly ulong[] PHI = [
				0x9E3779B97F4A7C15, 0xF39CC0605CEDC834, 0x1082276BF3A27251, 0xF86C6A11D0C18E95,
				0x2767F0B153D27B7F, 0x0347045B5BF1827F, 0x01886F0928403002, 0xC1D64BA40F335E36
			];
			state.state0 = Vector256.Create(PHI[00] ^ seed0, PHI[01], PHI[02] ^ seed1, PHI[03]);
			state.state1 = Vector256.Create(PHI[04] ^ seed2, PHI[05], PHI[06] ^ seed3, PHI[07]);
			*/

			state.state0 = Vector256.Create(0x9E3779B97F4A7C15 ^ seed0, 0xF39CC0605CEDC834, 0x1082276BF3A27251 ^ seed1, 0xF86C6A11D0C18E95);
			state.state1 = Vector256.Create(0x2767F0B153D27B7F ^ seed2, 0x0347045B5BF1827F, 0x01886F0928403002 ^ seed3, 0xC1D64BA40F335E36);
			for (int i = 0; i < ROUNDS; i++) {
				Generate(ref state, null, 32 * STEPS);
				state.state0 = state.state1;
				state.state1 = state.output;
			}
			return state;
		}

		/// <summary>
		/// The pRNG state stores all values needed to iterate. This state is opaque to prevent
		/// compromising the randomizer.
		/// </summary>
		public unsafe struct PrngState {
			internal Vector256<ulong> state0;
			internal Vector256<ulong> state1;
			internal Vector256<ulong> output;
			internal Vector256<ulong> counter;
		}
	}
}
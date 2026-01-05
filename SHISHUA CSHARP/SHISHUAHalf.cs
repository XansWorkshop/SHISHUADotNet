using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace SHISHUADotNet {

	/// <summary>
	/// An implementation of the <see href="https://github.com/espadrine/shishua">SHISHUA</see> pRNG algorithm, an extremely fast and resilient
	/// pRNG algorithm suitable for generating enormous blocks of data.
	/// <para/>
	/// This is the "half-size" implementation. It is simpler than its full-size counterpart, exporting blocks of 32 bytes instead of the 128,
	/// but in exchange it also runs at half speed.
	/// </summary>
	/// <remarks>
	/// Note that this is not a drop-in replacement for <see cref="SHISHUA"/>, because this variation of the algorithm will generate different data
	/// even if the seed is the same.
	/// </remarks>
	public class SHISHUAHalf {

		/// <summary>
		/// Generates a specified amount of bytes using the provided state. The result buffer can be <see langword="null"/> to only advance the randomizer
		/// without storing its output. The amount of bytes must be divisible by 32.
		/// </summary>
		/// <param name="state">The randomizer state.</param>
		/// <param name="resultBuffer">The output buffer to store generated random bytes into. Can be <see langword="null"/> to skip storing data and advance the state anyway.</param>
		/// <param name="generationSize">The amount of bytes to generate. If the <paramref name="resultBuffer"/> is not <see langword="null"/> (or, empty), this must be greater than or equal to its size. Must be divisible by 32.</param>
		/// <exception cref="ArgumentException">The <paramref name="resultBuffer"/> is not empty, but has a length less than <paramref name="generationSize"/>, or the <paramref name="generationSize"/> is not divisible by 32.</exception>
		/// <exception cref="PlatformNotSupportedException">The current hardware is not able to perform all of the operations necessary to leverage this randomizer.</exception>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static void Generate(ref PrngState state, Span<byte> resultBuffer, int generationSize) {
			if (!resultBuffer.IsEmpty) {
				if (resultBuffer.Length < generationSize) throw new ArgumentException($"The {nameof(generationSize)} parameter must be greater than or equal to {nameof(resultBuffer)}.Length");
			}
			if ((generationSize & 0x1F) != 0) throw new ArgumentException($"The {nameof(generationSize)} parameter must be divisible by 32.");

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
				if (resultBuffer.Length - i >= 32) {
					o.AsByte().CopyTo(resultBuffer.Slice(i, 32));
				}

				s1 += counter;
				counter += increment;

				u0 = s0 >>> 1;
				u1 = s1 >>> 3;

				t0 = V256Helper.Extract20(s0.AsByte()).AsUInt64();
				t1 = V256Helper.Extract12(s1.AsByte()).AsUInt64();

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
		/// Initializes the randomizer state to the provided 256 bit seed.
		/// </summary>
		/// <param name="seed0">The first 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed1">The second 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed2">The third 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed3">The fourth 64 of 256 bits needed to create a seed.</param>
		public static PrngState Initialize(ulong seed0, ulong seed1, ulong seed2, ulong seed3) {
			const int STEPS = 5;
			const int ROUNDS = 4;

			PrngState state = default;
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
		public struct PrngState {
			internal Vector256<ulong> state0;
			internal Vector256<ulong> state1;
			internal Vector256<ulong> output;
			internal Vector256<ulong> counter;
		}
	}
}
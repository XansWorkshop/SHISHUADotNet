using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace SHISHUADotNet {

	/// <summary>
	/// An implementation of the <see href="https://github.com/espadrine/shishua">SHISHUA</see> pRNG algorithm, an extremely fast and resilient
	/// pRNG algorithm suitable for generating enormous blocks of data.
	/// </summary>
	public class SHISHUA {

		/// <summary>
		/// Generates an amount of bytes using the provided state.
		/// </summary>
		/// <param name="state">The randomizer state.</param>
		/// <param name="resultBuffer">The output buffer to store generated random bytes into. Can be <see langword="null"/> to skip storing data and advance the state anyway.</param>
		/// <param name="generationSize">The amount of bytes to generate. If the <paramref name="resultBuffer"/> is not <see langword="null"/> (or, empty), this must be greater than or equal to its size. Must be divisible by 128.</param>
		/// <exception cref="InvalidOperationException"></exception>
		public static void Generate(ref PrngState state, Span<byte> resultBuffer, int generationSize) {
			if (!resultBuffer.IsEmpty) {
				if (resultBuffer.Length < generationSize) throw new ArgumentException($"The {nameof(generationSize)} parameter must be greater than or equal to {nameof(resultBuffer)}.Length");
				if (generationSize % 128 != 0) throw new ArgumentException($"The {nameof(generationSize)} parameter (and by extension {nameof(resultBuffer)}.Length) must be divisible by 128.");
			}
			Vector256<ulong> o0 = state.output0;
			Vector256<ulong> o1 = state.output1;
			Vector256<ulong> o2 = state.output2;
			Vector256<ulong> o3 = state.output3;
			Vector256<ulong> s0 = state.state0;
			Vector256<ulong> s1 = state.state1;
			Vector256<ulong> s2 = state.state2;
			Vector256<ulong> s3 = state.state3;
			Vector256<ulong> counter = state.counter;
			Vector256<ulong> t0 = default;
			Vector256<ulong> t1 = default;
			Vector256<ulong> t2 = default;
			Vector256<ulong> t3 = default;
			Vector256<ulong> u0 = default;
			Vector256<ulong> u1 = default;
			Vector256<ulong> u2 = default;
			Vector256<ulong> u3 = default;

			Vector256<ulong> increment = Vector256.Create(7UL, 5UL, 3UL, 1UL);

			for (int i = 0; i < generationSize; i += 128) {
				if (resultBuffer.Length - i >= 128) {
					Span<byte> blockSpan = resultBuffer.Slice(i, 128);
					o0.AsByte().CopyTo(blockSpan.Slice(00, 32));
					o1.AsByte().CopyTo(blockSpan.Slice(32, 32));
					o2.AsByte().CopyTo(blockSpan.Slice(64, 32));
					o3.AsByte().CopyTo(blockSpan.Slice(96, 32));
				}

				s1 += counter;
				s3 += counter;
				counter += increment;

				u0 = s0 >>> 1;
				u1 = s1 >>> 3;
				u2 = s2 >>> 1;
				u3 = s3 >>> 3;

				// NO NO NO NO NO!
				// Do NOT!
				// Vector256<int> shu0 = Vector256.Create(5, 6, 7, 0, 1, 2, 3, 4);
				// Vector256<int> shu1 = Vector256.Create(3, 4, 5, 6, 7, 0, 1, 2);

				// Reason: Storing this in a local variable causes JIT to get *really* defensive about the code, assuming the worst
				// (aka "this value might change"). This causes it to generate assembly that manually performs the permutation using
				// scalar code, because ordinarily the expected AVX2 vpermd instruction requires the permutation operator to be a
				// constant (like, an instruction operator). For obvious reasons, a local variable that may or may not change isn't
				// a constant that can be put into the bytecode.

				// For this reason, you *must* put the parameter into the method call directly. This tells JIT that we have
				// no intent of changing the value ever, and so it can emit the properly optimized vpermd instruction on
				// hardware that supports AVX2.

				t0 = Vector256.Shuffle(s0.AsInt32(), Vector256.Create(5, 6, 7, 0, 1, 2, 3, 4)).AsUInt64();
				t1 = Vector256.Shuffle(s1.AsInt32(), Vector256.Create(3, 4, 5, 6, 7, 0, 1, 2)).AsUInt64();
				t2 = Vector256.Shuffle(s2.AsInt32(), Vector256.Create(5, 6, 7, 0, 1, 2, 3, 4)).AsUInt64();
				t3 = Vector256.Shuffle(s3.AsInt32(), Vector256.Create(3, 4, 5, 6, 7, 0, 1, 2)).AsUInt64();

				s0 = t0 + u0;
				s1 = t1 + u1;
				s2 = t2 + u2;
				s3 = t3 + u3;

				o0 = Vector256.Xor(u0, t1);
				o1 = Vector256.Xor(u2, t3);
				o2 = Vector256.Xor(s0, s3);
				o3 = Vector256.Xor(s2, s1);
			}
			state.output0 = o0;
			state.output1 = o1;
			state.output2 = o2;
			state.output3 = o3;
			state.state0 = s0;
			state.state1 = s1;
			state.state2 = s2;
			state.state3 = s3;
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
			const int STEPS = 1;
			const int ROUNDS = 13;

			PrngState state = default;
			/*
			state.state0 = Vector256.Create(PHI[00] ^ seed0, PHI[01], PHI[02] ^ seed1, PHI[03]);
			state.state1 = Vector256.Create(PHI[04] ^ seed2, PHI[05], PHI[06] ^ seed3, PHI[07]);
			state.state2 = Vector256.Create(PHI[08] ^ seed2, PHI[09], PHI[10] ^ seed3, PHI[11]);
			state.state3 = Vector256.Create(PHI[12] ^ seed0, PHI[13], PHI[14] ^ seed1, PHI[15]);

			private static readonly ulong[] PHI = [
				0x9E3779B97F4A7C15, 0xF39CC0605CEDC834, 0x1082276BF3A27251, 0xF86C6A11D0C18E95,
				0x2767F0B153D27B7F, 0x0347045B5BF1827F, 0x01886F0928403002, 0xC1D64BA40F335E36,
				0xF06AD7AE9717877E, 0x85839D6EFFBD7DC6, 0x64D325D1C5371682, 0xCADD0CCCFDFFBBE1,
				0x626E33B8D04B4331, 0xBBF73C790D94F79D, 0x471C4AB3ED3D82A5, 0xFEC507705E4AE6E5
			];
			*/

			// Put these into source. Yes magic number bad.
			// But also it wastes time in a particularly performance-sensitive piece of code.
			state.state0 = Vector256.Create(0x9E3779B97F4A7C15 ^ seed0, 0xF39CC0605CEDC834, 0x1082276BF3A27251 ^ seed1, 0xF86C6A11D0C18E95);
			state.state1 = Vector256.Create(0x2767F0B153D27B7F ^ seed2, 0x0347045B5BF1827F, 0x01886F0928403002 ^ seed3, 0xC1D64BA40F335E36);
			state.state2 = Vector256.Create(0xF06AD7AE9717877E ^ seed2, 0x85839D6EFFBD7DC6, 0x64D325D1C5371682 ^ seed3, 0xCADD0CCCFDFFBBE1);
			state.state3 = Vector256.Create(0x626E33B8D04B4331 ^ seed0, 0xBBF73C790D94F79D, 0x471C4AB3ED3D82A5 ^ seed1, 0xFEC507705E4AE6E5);
			for (int i = 0; i < ROUNDS; i++) {
				Generate(ref state, null, 128 * STEPS);
				state.state0 = state.output3; 
				state.state1 = state.output2;
				state.state2 = state.output1; 
				state.state3 = state.output0;
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
			internal Vector256<ulong> state2;
			internal Vector256<ulong> state3;

			internal Vector256<ulong> output0;
			internal Vector256<ulong> output1;
			internal Vector256<ulong> output2;
			internal Vector256<ulong> output3;

			internal Vector256<ulong> counter;
		}
	}

}
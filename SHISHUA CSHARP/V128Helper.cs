using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace SHISHUADotNet {
	#pragma warning disable CA1857 // Affects ShiftLeftLogical128BitLane call; the amount is already expected to be effectively constant so this is fine.

	/// <summary>
	/// Contains optimizations for specific 128 bit operations used by <see cref="SHISHUA"/> and <see cref="SHISHUAHalf"/>.
	/// </summary>
	internal static class V128Helper {

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector128<byte> AlignRight(Vector128<byte> lower, Vector128<byte> upper, [ConstantExpected(Min = 0, Max = 15)] byte amount) {
			if (AdvSimd.IsSupported) {
				return AdvSimd.ExtractVector128(lower, upper, amount);
			}

			if (Ssse3.IsSupported) {
				return Ssse3.AlignRight(upper, lower, amount);
			}

			if (Sse2.IsSupported) {
				Vector128<byte> a = Sse2.ShiftLeftLogical128BitLane(upper, (byte)(16 - amount));
				Vector128<byte> b = Sse2.ShiftRightLogical128BitLane(lower, amount);
				return a | b;
			}

			ThrowPlatformNotSupported();
			return default;
		}

		[DoesNotReturn]
		private static void ThrowPlatformNotSupported() {
			throw new PlatformNotSupportedException();
		}
	}
}
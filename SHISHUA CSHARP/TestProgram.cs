#if !RELEASE
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SHISHUADotNet {
	public class TestProgram {
		private static void WriteByteArray(Span<byte> buf) {
			for (int i = 0; i < buf.Length; i++) {
				Console.Write($"{buf[i]:X2}");
			}
			Console.WriteLine();
		}
		
		public static void Main() {
			Span<byte> result = stackalloc byte[256];
			SHISHUA.PrngState state = SHISHUA.Initialize(0xDEADBEEF, 0x69420, 0x123456789101112, 0x13371337);
			SHISHUAHalf.PrngState stateHalf = SHISHUAHalf.Initialize(0xDEADBEEF, 0x69420, 0x123456789101112, 0x13371337);

			SHISHUA.Generate(ref state, result, 256);
			WriteByteArray(result);
			Console.WriteLine();
			SHISHUAHalf.Generate(ref stateHalf, result, 256);
			WriteByteArray(result);

			/*
			ProcessStartInfo rngTest = new ProcessStartInfo(
				"RNG_test.exe",
				"stdin -a -multithreaded -tlfail"
			) {
				RedirectStandardInput = true,
			};

			try {
				Process? rng = Process.Start(rngTest);
				if (rng != null) {
					ulong remainingBytes = 1099511627776;
					using BinaryWriter writer = new BinaryWriter(rng.StandardInput.BaseStream);
					while (remainingBytes > 0) {
						SHISHUA.Generate(ref state, result, 16384);
						remainingBytes -= 16384;
						writer.Write(result);
					}
				}
			} catch { }
			*/
		}
	}
}
#endif
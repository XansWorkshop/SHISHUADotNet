using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace SHISHUADotNet;

internal static class V256Helper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<byte> Extract12(Vector256<byte> vector)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            return Vector256.Shuffle(vector.AsInt32(), Vector256.Create(3, 4, 5, 6, 7, 0, 1, 2)).AsByte();
        }
        return Fallback(vector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<byte> Fallback(Vector256<byte> vector)
        {
            Vector128<byte> lo = vector.GetLower();
            Vector128<byte> hi = vector.GetUpper();

            Vector128<byte> a = V128Helper.AlignRight(lo, hi, 12);
            Vector128<byte> b = V128Helper.AlignRight(hi, lo, 12);

            return Vector256.Create(a, b);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<byte> Extract20(Vector256<byte> vector)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            return Vector256.Shuffle(vector.AsInt32(), Vector256.Create(5, 6, 7, 0, 1, 2, 3, 4)).AsByte();
        }
        return Fallback(vector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<byte> Fallback(Vector256<byte> vector)
        {
            Vector128<byte> lo = vector.GetLower();
            Vector128<byte> hi = vector.GetUpper();

            Vector128<byte> a = V128Helper.AlignRight(hi, lo, 4);
            Vector128<byte> b = V128Helper.AlignRight(lo, hi, 4);

            return Vector256.Create(a, b);
        }
    }
}

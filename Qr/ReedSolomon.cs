namespace HomeStream.Qr;

internal static class ReedSolomon
{
    private static readonly int[] GfExp = new int[512];
    private static readonly int[] GfLog = new int[256];

    static ReedSolomon()
    {
        int x = 1;
        for (int i = 0; i < 255; i++)
        {
            GfExp[i] = x;
            GfLog[x] = i;
            x <<= 1;
            if ((x & 0x100) != 0) x ^= 0x11D;
        }
        for (int i = 255; i < 512; i++) GfExp[i] = GfExp[i - 255];
    }

    private static int GfMul(int a, int b)
    {
        if (a == 0 || b == 0) return 0;
        return GfExp[(GfLog[a] + GfLog[b]) % 255];
    }

    public static int[] GetEcBytes(int[] data, int ecCount)
    {
        // Build generator polynomial
        int[] gen = new int[] { 1 };
        for (int i = 0; i < ecCount; i++)
        {
            int[] term = new int[] { 1, GfExp[i] };
            int[] newGen = new int[gen.Length + 1];
            for (int j = 0; j < gen.Length; j++)
                for (int k = 0; k < term.Length; k++)
                    newGen[j + k] ^= GfMul(gen[j], term[k]);
            gen = newGen;
        }

        // Polynomial division (remainder = EC codewords)
        int[] msg = new int[data.Length + ecCount];
        Array.Copy(data, msg, data.Length);
        for (int i = 0; i < data.Length; i++)
        {
            int coef = msg[i];
            if (coef == 0) continue;
            for (int j = 1; j < gen.Length; j++)
                msg[i + j] ^= GfMul(gen[j], coef);
        }

        int[] ec = new int[ecCount];
        Array.Copy(msg, data.Length, ec, 0, ecCount);
        return ec;
    }
}

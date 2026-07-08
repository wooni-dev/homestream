using System.Text;

namespace HomeStream.Qr;

internal static class QrEncoder
{
    // ECC level M capacity in bytes per version (index = version)
    private static readonly int[] CapacityM = { 0, 14, 26, 42, 62, 84 };
    // EC codewords per block for ECC level M, per version
    private static readonly int[] EcPerBlock = { 0, 10, 16, 26, 18, 24 };
    // Number of EC blocks for ECC level M, per version
    private static readonly int[] EcBlocks = { 0, 1, 1, 1, 2, 2 };
    // Total codewords per version
    private static readonly int[] TotalCw = { 0, 26, 44, 70, 100, 134 };

    public static (int[] dataCodewords, int version, int ecCount) Encode(string url)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(url);
        int version = 1;
        for (; version <= 5; version++)
            if (CapacityM[version] >= bytes.Length) break;
        if (version > 5) throw new ArgumentException("URL too long for QR version 1-5");

        int totalCw = TotalCw[version];
        int numBlocks = EcBlocks[version];
        int ecCwPerBlock = EcPerBlock[version];
        int dataCw = totalCw - numBlocks * ecCwPerBlock;

        // Build bit buffer
        var bits = new List<bool>();
        // Mode indicator: Byte = 0100
        AddBits(bits, 0b0100, 4);
        // Character count (8 bits for version 1-9)
        AddBits(bits, bytes.Length, 8);
        // Data bytes
        foreach (byte b in bytes) AddBits(bits, b, 8);
        // Terminator
        for (int i = 0; i < 4 && bits.Count < dataCw * 8; i++) bits.Add(false);
        // Pad to byte boundary
        while (bits.Count % 8 != 0) bits.Add(false);
        // Padding codewords
        int padIdx = 0;
        int[] pads = { 0xEC, 0x11 };
        while (bits.Count < dataCw * 8)
        {
            AddBits(bits, pads[padIdx % 2], 8);
            padIdx++;
        }

        // Convert bits to codewords
        int[] codewords = new int[dataCw];
        for (int i = 0; i < dataCw; i++)
        {
            int val = 0;
            for (int j = 0; j < 8; j++)
                if (bits[i * 8 + j]) val |= (1 << (7 - j));
            codewords[i] = val;
        }

        return (codewords, version, ecCwPerBlock);
    }

    private static void AddBits(List<bool> bits, int value, int count)
    {
        for (int i = count - 1; i >= 0; i--)
            bits.Add(((value >> i) & 1) == 1);
    }
}

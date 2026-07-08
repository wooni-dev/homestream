namespace HomeStream.Qr;

internal static class QrCode
{
    public static bool[,] Encode(string url)
    {
        var (dataCw, version, ecCount) = QrEncoder.Encode(url);

        // Interleave data + EC codewords (single block for versions 1-3, 2 blocks for 4-5)
        int numBlocks = version <= 3 ? 1 : 2;
        int[] allCw = InterleaveBlocks(dataCw, ecCount, numBlocks);

        int n = 17 + 4 * version;
        var mat = new sbyte[n, n]; // 0=white, 1=black, -1=reserved
        var func = new bool[n, n]; // functional modules (not data)

        PlaceFinders(mat, func, n);
        PlaceTimings(mat, func, n);
        if (version >= 2) PlaceAlignment(mat, func, version, n);
        ReserveFormat(mat, func, n);

        // Place data bits
        PlaceData(mat, func, allCw, n);

        // Choose best mask
        int bestMask = 0;
        int bestPenalty = int.MaxValue;
        bool[,]? bestMatrix = null;
        for (int m = 0; m < 8; m++)
        {
            var candidate = ApplyMask(mat, func, n, m);
            WriteFormat(candidate, n, m);
            int penalty = CalcPenalty(candidate, n);
            if (penalty < bestPenalty)
            {
                bestPenalty = penalty;
                bestMask = m;
                bestMatrix = candidate;
            }
        }

        // Add 4-module quiet zone border
        int nb = n + 8;
        var result = new bool[nb, nb];
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                result[r + 4, c + 4] = bestMatrix![r, c];

        return result;
    }

    private static void PlaceFinders(sbyte[,] mat, bool[,] func, int n)
    {
        PlaceFinder(mat, func, 0, 0);
        PlaceFinder(mat, func, 0, n - 7);
        PlaceFinder(mat, func, n - 7, 0);

        // Separators
        for (int i = 0; i < 8; i++)
        {
            Set(mat, func, 7, i, 0); Set(mat, func, i, 7, 0);
            Set(mat, func, 7, n - 8 + i, 0); Set(mat, func, i, n - 8, 0);
            Set(mat, func, n - 8, i, 0); Set(mat, func, n - 8 + i, 7, 0);
        }
    }

    private static void PlaceFinder(sbyte[,] mat, bool[,] func, int row, int col)
    {
        int[][] rows = {
            new[]{1,1,1,1,1,1,1},
            new[]{1,0,0,0,0,0,1},
            new[]{1,0,1,1,1,0,1},
            new[]{1,0,1,1,1,0,1},
            new[]{1,0,1,1,1,0,1},
            new[]{1,0,0,0,0,0,1},
            new[]{1,1,1,1,1,1,1},
        };
        for (int r = 0; r < 7; r++)
            for (int c = 0; c < 7; c++)
                Set(mat, func, row + r, col + c, (sbyte)rows[r][c]);
    }

    private static void PlaceTimings(sbyte[,] mat, bool[,] func, int n)
    {
        for (int i = 8; i < n - 8; i++)
        {
            sbyte v = (sbyte)(i % 2 == 0 ? 1 : 0);
            Set(mat, func, 6, i, v);
            Set(mat, func, i, 6, v);
        }
    }

    private static void PlaceAlignment(sbyte[,] mat, bool[,] func, int version, int n)
    {
        int[] pos = version switch
        {
            2 => new[] { 6, 18 },
            3 => new[] { 6, 22 },
            4 => new[] { 6, 26 },
            5 => new[] { 6, 30 },
            _ => Array.Empty<int>()
        };
        foreach (int r in pos)
            foreach (int c in pos)
            {
                if (func[r, c]) continue; // overlaps finder
                for (int dr = -2; dr <= 2; dr++)
                    for (int dc = -2; dc <= 2; dc++)
                    {
                        int ar = Math.Abs(dr), ac = Math.Abs(dc);
                        sbyte v = (ar == 2 || ac == 2 || (ar == 0 && ac == 0)) ? (sbyte)1 : (sbyte)0;
                        Set(mat, func, r + dr, c + dc, v);
                    }
            }
    }

    private static void ReserveFormat(sbyte[,] mat, bool[,] func, int n)
    {
        for (int i = 0; i < 9; i++)
        {
            if (i != 6) Reserve(mat, func, 8, i); // (8,6) = timing module, keep dark
            if (i != 6) Reserve(mat, func, i, 8); // (6,8) = timing module, keep dark
        }
        for (int i = 0; i < 8; i++)
        {
            Reserve(mat, func, 8, n - 1 - i);
            Reserve(mat, func, n - 1 - i, 8);
        }
        Set(mat, func, 8, n - 8, 1); // dark module
    }

    private static void PlaceData(sbyte[,] mat, bool[,] func, int[] cw, int n)
    {
        int idx = 0, bit = 7;
        bool upward = true;
        for (int col = n - 1; col >= 1; col -= 2)
        {
            if (col == 6) col--; // skip timing column
            for (int i = 0; i < n; i++)
            {
                int r = upward ? n - 1 - i : i;
                for (int dc = 0; dc < 2; dc++)
                {
                    int c = col - dc;
                    if (func[r, c]) continue;
                    bool b = idx < cw.Length && ((cw[idx] >> bit) & 1) == 1;
                    mat[r, c] = b ? (sbyte)1 : (sbyte)0;
                    bit--;
                    if (bit < 0) { bit = 7; idx++; }
                }
            }
            upward = !upward;
        }
    }

    private static bool[,] ApplyMask(sbyte[,] mat, bool[,] func, int n, int maskIdx)
    {
        var result = new bool[n, n];
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
            {
                bool val = mat[r, c] == 1;
                if (!func[r, c] && MaskCondition(maskIdx, r, c))
                    val = !val;
                result[r, c] = val;
            }
        return result;
    }

    private static bool MaskCondition(int mask, int r, int c) => mask switch
    {
        0 => (r + c) % 2 == 0,
        1 => r % 2 == 0,
        2 => c % 3 == 0,
        3 => (r + c) % 3 == 0,
        4 => (r / 2 + c / 3) % 2 == 0,
        5 => (r * c) % 2 + (r * c) % 3 == 0,
        6 => ((r * c) % 2 + (r * c) % 3) % 2 == 0,
        7 => ((r + c) % 2 + (r * c) % 3) % 2 == 0,
        _ => false
    };

    // Format info bits for ECC level M (bits 13-12) = 00
    private static readonly int[] FormatInfoM = {
        0b101010000010010, 0b101000100100101, 0b101111001111100, 0b101101101001011,
        0b100010111111001, 0b100000011001110, 0b100111110010111, 0b100101010100000
    };

    private static void WriteFormat(bool[,] mat, int n, int mask)
    {
        int fmt = FormatInfoM[mask];

        // First copy: near top-left finder. Bit 14 (MSB) nearest the finder, descending.
        for (int i = 0; i <= 5; i++)
            mat[8, i] = ((fmt >> (14 - i)) & 1) == 1; // cols 0-5: bits 14-9
        mat[8, 7] = ((fmt >> 8) & 1) == 1; // col 7: bit 8
        mat[8, 8] = ((fmt >> 7) & 1) == 1; // col 8: bit 7
        mat[7, 8] = ((fmt >> 6) & 1) == 1; // row 7, col 8: bit 6
        for (int i = 0; i <= 5; i++)        // col 8, rows 0-5: bits 0-5
            mat[i, 8] = ((fmt >> i) & 1) == 1;

        // Second copy: bottom-left (col 8) continuing to top-right (row 8), same bit order
        for (int i = 0; i <= 6; i++)        // col 8, rows n-1 down to n-7: bits 14-8
            mat[n - 1 - i, 8] = ((fmt >> (14 - i)) & 1) == 1;
        mat[8, n - 8] = ((fmt >> 7) & 1) == 1; // col n-8: bit 7
        for (int i = 0; i <= 6; i++)        // row 8, cols n-7 to n-1: bits 6-0
            mat[8, n - 7 + i] = ((fmt >> (6 - i)) & 1) == 1;

        mat[n - 8, 8] = true; // dark module (always 1) — spec position is (n-8, 8), not (8, n-8)
    }

    private static int CalcPenalty(bool[,] mat, int n)
    {
        int penalty = 0;

        // Rule 1: 5+ consecutive same-color modules
        for (int r = 0; r < n; r++)
        {
            int run = 1;
            for (int c = 1; c < n; c++)
            {
                if (mat[r, c] == mat[r, c - 1]) run++;
                else { if (run >= 5) penalty += 3 + run - 5; run = 1; }
            }
            if (run >= 5) penalty += 3 + run - 5;
        }
        for (int c = 0; c < n; c++)
        {
            int run = 1;
            for (int r = 1; r < n; r++)
            {
                if (mat[r, c] == mat[r - 1, c]) run++;
                else { if (run >= 5) penalty += 3 + run - 5; run = 1; }
            }
            if (run >= 5) penalty += 3 + run - 5;
        }

        // Rule 2: 2x2 blocks
        for (int r = 0; r < n - 1; r++)
            for (int c = 0; c < n - 1; c++)
                if (mat[r, c] == mat[r, c + 1] && mat[r, c] == mat[r + 1, c] && mat[r, c] == mat[r + 1, c + 1])
                    penalty += 3;

        // Rule 3: finder-like patterns
        bool[] p1 = { true, false, true, true, true, false, true, false, false, false, false };
        bool[] p2 = { false, false, false, false, true, false, true, true, true, false, true };
        for (int r = 0; r < n; r++)
            for (int c = 0; c <= n - 11; c++)
            {
                bool m1 = true, m2 = true;
                for (int k = 0; k < 11; k++)
                {
                    if (mat[r, c + k] != p1[k]) m1 = false;
                    if (mat[r, c + k] != p2[k]) m2 = false;
                }
                if (m1 || m2) penalty += 40;
            }
        for (int c = 0; c < n; c++)
            for (int r = 0; r <= n - 11; r++)
            {
                bool m1 = true, m2 = true;
                for (int k = 0; k < 11; k++)
                {
                    if (mat[r + k, c] != p1[k]) m1 = false;
                    if (mat[r + k, c] != p2[k]) m2 = false;
                }
                if (m1 || m2) penalty += 40;
            }

        // Rule 4: dark module ratio
        int dark = 0;
        for (int r = 0; r < n; r++) for (int c = 0; c < n; c++) if (mat[r, c]) dark++;
        int total = n * n;
        int pct = dark * 100 / total;
        int prev5 = (pct / 5) * 5, next5 = prev5 + 5;
        penalty += Math.Min(Math.Abs(prev5 - 50), Math.Abs(next5 - 50)) / 5 * 10;

        return penalty;
    }

    private static int[] InterleaveBlocks(int[] dataCw, int ecCount, int numBlocks)
    {
        if (numBlocks == 1)
        {
            int[] ec = ReedSolomon.GetEcBytes(dataCw, ecCount);
            int[] all = new int[dataCw.Length + ec.Length];
            Array.Copy(dataCw, all, dataCw.Length);
            Array.Copy(ec, 0, all, dataCw.Length, ec.Length);
            return all;
        }

        // Split data into blocks
        int blockSize = dataCw.Length / numBlocks;
        var blocks = new int[numBlocks][];
        var ecBlocks = new int[numBlocks][];
        for (int i = 0; i < numBlocks; i++)
        {
            blocks[i] = dataCw.Skip(i * blockSize).Take(blockSize).ToArray();
            ecBlocks[i] = ReedSolomon.GetEcBytes(blocks[i], ecCount);
        }

        var result = new List<int>();
        for (int i = 0; i < blockSize; i++)
            for (int b = 0; b < numBlocks; b++)
                result.Add(blocks[b][i]);
        for (int i = 0; i < ecCount; i++)
            for (int b = 0; b < numBlocks; b++)
                result.Add(ecBlocks[b][i]);
        return result.ToArray();
    }

    private static void Set(sbyte[,] mat, bool[,] func, int r, int c, sbyte v)
    {
        mat[r, c] = v; func[r, c] = true;
    }

    private static void Reserve(sbyte[,] mat, bool[,] func, int r, int c)
    {
        mat[r, c] = 0; func[r, c] = true;
    }
}

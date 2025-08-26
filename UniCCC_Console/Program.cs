using System.Text;
using System.Text.RegularExpressions;

// --- ヘルプ ---
if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
{
    Console.WriteLine("""
    UniCCC - Unicode → UTF-16BE converter
    Usage:
      dotnet run -- [options] <tokens...>

    Tokens:
      U+XXXX   (例: U+0041)
      0xXXXX   (例: 0x1F600)
      &#xHH;   (例: &#x1F600;)
      &#DDDD;  (例: &#128512;)
      65       (10進数)
      1F600    (裸の16進数)

    Options:
      -o <file>        UTF-16BE バイナリをファイルに出力
      --hex-only | -H  標準出力に16進バイト列だけ表示
      -h, --help       このヘルプを表示
    """);
    // return;
}

// --- オプション解析 ---
string? outFile = null;
bool hexOnly = false;
List<string> tokens = new();

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-o":
            if (i + 1 < args.Length) outFile = args[++i];
            else Console.Error.WriteLine("Error: -o requires a filename.");
            break;
        case "--hex-only":
        case "-H":
            hexOnly = true;
            break;
        default:
            tokens.Add(args[i]);
            break;
    }
}

// --- トークン → コードポイント ---
(List<int> cps, List<string> errors) = ParseTokensToCodePoints(tokens);

if (errors.Count > 0)
{
    Console.Error.WriteLine("Parse errors:");
    foreach (var e in errors) Console.Error.WriteLine("  " + e);
}

if (cps.Count == 0)
{
    Console.Error.WriteLine("No valid code points.");
    return 1;
}

// --- UTF-16BE 変換 ---
List<byte> bytes = new();
foreach (var cp in cps)
{
    if (cp <= 0xFFFF)
    {
        if (cp is >= 0xD800 and <= 0xDFFF)
            Console.Error.WriteLine($"Warning: U+{cp:X4} is a surrogate code unit.");
        bytes.Add((byte)((cp >> 8) & 0xFF));
        bytes.Add((byte)(cp & 0xFF));
    }
    else
    {
        int v = cp - 0x10000;
        int high = 0xD800 | ((v >> 10) & 0x3FF);
        int low  = 0xDC00 | (v & 0x3FF);
        bytes.Add((byte)(high >> 8));
        bytes.Add((byte)(high & 0xFF));
        bytes.Add((byte)(low >> 8));
        bytes.Add((byte)(low & 0xFF));
    }
}

// --- ファイル出力 ---
if (outFile != null)
{
    File.WriteAllBytes(outFile, bytes.ToArray());
    Console.WriteLine($"Wrote {bytes.Count} bytes to '{outFile}' (UTF-16BE).");
}

// --- 出力表示 ---
string hex = string.Join(" ", bytes.Select(b => b.ToString("X2")));

if (hexOnly)
{
    Console.WriteLine(hex);
}
else
{
    Console.WriteLine("Code points: " + string.Join(" ", cps.Select(cp => $"U+{cp:X4}")));
    Console.WriteLine("String:      " + string.Concat(cps.Select(cp => char.ConvertFromUtf32(cp))));
    Console.WriteLine("UTF-16BE:    " + hex);
}

return 0;

// --- 関数: トークンをコードポイントに変換 ---
static (List<int>, List<string>) ParseTokensToCodePoints(IEnumerable<string> inputs)
{
    var cps = new List<int>();
    var errors = new List<string>();

    foreach (var t in inputs.SelectMany(s => Regex.Split(s, @"[,\s]+")).Where(s => !string.IsNullOrWhiteSpace(s)))
    {
        int? cp = null;
        if (Regex.Match(t, @"^U\+([0-9A-Fa-f]+)$") is { Success: true } m1)
            cp = Convert.ToInt32(m1.Groups[1].Value, 16);
        else if (Regex.Match(t, @"^0x([0-9A-Fa-f]+)$") is { Success: true } m2)
            cp = Convert.ToInt32(m2.Groups[1].Value, 16);
        else if (Regex.Match(t, @"^&#x([0-9A-Fa-f]+);?$") is { Success: true } m3)
            cp = Convert.ToInt32(m3.Groups[1].Value, 16);
        else if (Regex.Match(t, @"^&#([0-9]+);?$") is { Success: true } m4)
            cp = Convert.ToInt32(m4.Groups[1].Value, 10);
        else if (Regex.IsMatch(t, @"^[0-9]+$"))
            cp = Convert.ToInt32(t, 10);
        else if (Regex.IsMatch(t, @"^[0-9A-Fa-f]+$"))
            cp = Convert.ToInt32(t, 16);
        else
            errors.Add($"Unrecognized token: '{t}'");

        if (cp is { } val)
        {
            if (val < 0 || val > 0x10FFFF)
                errors.Add($"Out of range: {t} -> {val}");
            else
                cps.Add(val);
        }
    }

    return (cps, errors);
}

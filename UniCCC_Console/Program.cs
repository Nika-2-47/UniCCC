using System;
using System.Text;

class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Write("1: 文字→コードポイント, 2: コードポイント→文字, q: 終了: ");
            string choice = Console.ReadLine();

            if (choice == "1")
                CharToCodePoint();
            else if (choice == "2")
                CodePointToChar();
            else if (choice?.ToLower() == "q")
                break;
            else
                Console.WriteLine("無効な選択です。");
        }
    }

    static void CharToCodePoint()
    {
        Console.Write("文字を入力してください: ");
        string input = Console.ReadLine();
        if (string.IsNullOrEmpty(input)) return;

        var enumerator = input.EnumerateRunes();
        foreach (var rune in enumerator)
        {
            Console.WriteLine($"'{rune}' → U+{rune.Value:X}");
        }
    }

    static void CodePointToChar()
    {
        Console.Write("コードポイントを入力してください（例: 1F600）: ");
        string input = Console.ReadLine();
        if (string.IsNullOrEmpty(input)) return;

        try
        {
            int codePoint = int.Parse(input, System.Globalization.NumberStyles.HexNumber);
            if (Rune.TryCreate(codePoint, out Rune rune))
            {
                Console.WriteLine($"U+{codePoint:X} → '{rune}'");
            }
            else
            {
                Console.WriteLine("無効なコードポイントです。");
            }
        }
        catch
        {
            Console.WriteLine("無効な入力です。");
        }
    }
}

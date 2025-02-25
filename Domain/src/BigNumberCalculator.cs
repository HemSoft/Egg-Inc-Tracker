namespace HemSoft.EggIncTracker.Domain;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

public static class BigNumberCalculator
{
    private static readonly Dictionary<string, int> SuffixRanks = new()
    {
        { "", 0 }, { "K", 1 }, { "M", 2 }, { "B", 3 }, { "T", 4 },
        { "q", 5 }, { "Q", 6 }, { "s", 7 }, { "S", 8 }, { "O", 9 },
        { "N", 10 }, { "d", 11 }, { "U", 12 }, { "D", 13 }, { "!", 14 }
    };

    private const int BaseValue = 1000; // Each rank represents a power of 1000

    public static string CalculateDifference(string num1, string num2)
    {
        BigInteger value1 = ParseBigNumber(num1);
        BigInteger value2 = ParseBigNumber(num2);

        BigInteger difference = BigInteger.Abs(value1 - value2);

        return FormatBigNumber(difference);
    }

    private static BigInteger ParseBigNumber(string bigNumber)
    {
        if (string.IsNullOrWhiteSpace(bigNumber))
            throw new ArgumentException("Input number cannot be null or empty.");

        // Extract suffix and numeric part
        string suffix = GetSuffix(bigNumber);
        string numericPart = bigNumber.Substring(0, bigNumber.Length - suffix.Length);

        if (!decimal.TryParse(numericPart, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalValue))
            throw new ArgumentException($"Invalid numeric part in: {bigNumber}");

        BigInteger baseValue = new BigInteger(decimalValue * (decimal)Math.Pow(10, SuffixRanks[suffix] * 3));
        return baseValue;
    }

    private static string GetSuffix(string input)
    {
        for (int i = input.Length - 1; i >= 0; i--)
        {
            if (char.IsLetter(input[i]) || input[i] == '!')
            {
                return input.Substring(i);
            }
        }
        return "";
    }

    private static string FormatBigNumber(BigInteger value)
    {
        if (value == 0) return "0";

        // Start from the largest suffix and work backwards
        var orderedSuffixes = SuffixRanks.OrderByDescending(x => x.Value);

        foreach (var suffix in orderedSuffixes)
        {
            BigInteger threshold = BigInteger.Pow(BaseValue, suffix.Value);
            if (value >= threshold)
            {
                decimal scaledValue = (decimal)value / (decimal)threshold;

                // Round to 3 decimal places
                scaledValue = Math.Round(scaledValue, 3);

                // Remove trailing zeros after decimal point
                string formatted = scaledValue.ToString("F3", CultureInfo.InvariantCulture)
                    .TrimEnd('0')
                    .TrimEnd('.');

                return $"{formatted}{suffix.Key}";
            }
        }

        // If value is less than 1000 (no suffix needed)
        return value.ToString();
    }
}

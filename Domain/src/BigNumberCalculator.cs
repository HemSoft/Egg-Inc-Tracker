namespace HemSoft.EggIncTracker.Domain;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

public static class BigNumberCalculator
{
    private static readonly Dictionary<char, BigInteger> Suffixes = new()
    {
        { 'k', BigInteger.Parse("1000") },
        { 'm', BigInteger.Parse("1000000") },
        { 'b', BigInteger.Parse("1000000000") },
        { 't', BigInteger.Parse("1000000000000") },
        { 'q', BigInteger.Parse("1000000000000000") },
        { 'Q', BigInteger.Parse("1000000000000000000") },
        { 's', BigInteger.Parse("1000000000000000000000") },
        { 'S', BigInteger.Parse("1000000000000000000000000") },
        { 'o', BigInteger.Parse("1000000000000000000000000000") },
        { 'n', BigInteger.Parse("1000000000000000000000000000000") },
        { 'd', BigInteger.Parse("1000000000000000000000000000000000") }
    };

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

    public static BigInteger ParseBigNumber(string bigNumber)
    {
        if (string.IsNullOrEmpty(bigNumber))
        {
            throw new ArgumentException("Input string cannot be null or empty.", nameof(bigNumber));
        }

        try
        {
            // Remove the '%' character if it exists at the end
            bigNumber = bigNumber.TrimEnd('%');

            char suffix = bigNumber[^1];
            if (Suffixes.ContainsKey(suffix))
            {
                string numberPart = bigNumber[..^1];
                if (double.TryParse(numberPart, out double number))
                {
                    return new BigInteger(number) * Suffixes[suffix];
                }
            }
            else
            {
                if (double.TryParse(bigNumber, out double number))
                {
                    return new BigInteger(number);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing big number: {bigNumber}. Exception: {ex}");
            throw;
        }

        throw new KeyNotFoundException($"The given key '{bigNumber[^1]}' was not present in the dictionary.");
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

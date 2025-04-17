namespace HemSoft.EggIncTracker.Domain;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

public static class BigNumberCalculator
{
    private static readonly Dictionary<char, BigInteger> Suffixes = new()
    {
        { 'k', BigInteger.Parse("1000") },                   // Thousand (10^3)
        { 'K', BigInteger.Parse("1000") },                   // Thousand (10^3)
        { 'm', BigInteger.Parse("1000000") },                // Million (10^6)
        { 'M', BigInteger.Parse("1000000") },                // Million (10^6)
        { 'b', BigInteger.Parse("1000000000") },             // Billion (10^9)
        { 'B', BigInteger.Parse("1000000000") },             // Billion (10^9)
        { 't', BigInteger.Parse("1000000000000") },          // Trillion (10^12)
        { 'T', BigInteger.Parse("1000000000000") },          // Trillion (10^12)
        { 'q', BigInteger.Parse("1000000000000000") },       // Quadrillion (10^15)
        { 'Q', BigInteger.Parse("1000000000000000000") },    // Quintillion (10^18)
        { 's', BigInteger.Parse("1000000000000000000000") }, // Sextillion (10^21)
        { 'S', BigInteger.Parse("1000000000000000000000000") }, // Septillion (10^24)
        { 'o', BigInteger.Parse("1000000000000000000000000000") }, // Octillion (10^27)
        { 'N', BigInteger.Parse("1000000000000000000000000000000") }, // Nonillion (10^30)
        { 'd', BigInteger.Parse("1000000000000000000000000000000000") }, // Decillion (10^33)
        { 'U', BigInteger.Parse("1000000000000000000000000000000000000") }, // Undecillion (10^36)
        { 'D', BigInteger.Parse("1000000000000000000000000000000000000000") } // Duodecillion (10^39)
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
                if (decimal.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number))
                {
                    // Convert to BigInteger without going through decimal conversion
                    var mantissa = new BigInteger((long)(number * 1000000)); // Preserve 6 decimal places
                    var suffixValue = Suffixes[suffix];
                    return (mantissa * suffixValue) / 1000000;
                }
            }
            else
            {
                if (decimal.TryParse(bigNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number))
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
        if (value == 0)
            return "0";

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

                // Format to 3 decimal places, keeping trailing zeros
                string formatted = scaledValue.ToString("F3", CultureInfo.InvariantCulture);
                // // Remove trailing zeros after decimal point -- Keep trailing zeros for consistency
                // formatted = formatted.TrimEnd('0').TrimEnd('.');

                return $"{formatted}{suffix.Key}";
            }
        }

        // If value is less than 1000 (no suffix needed)
        return value.ToString();
    }
}

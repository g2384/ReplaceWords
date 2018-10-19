using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReplaceWords
{
    public static class StringExtensions
    {
        public static IEnumerable<string> SplitCamelCase(this string value)
        {
            var words = Regex.Matches(value, "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)")
                .OfType<Match>()
                .Select(m => m.Value);
            return words;
        }

        public static IEnumerable<string> Split(this string value, string separator)
        {
            return value.Split(new[] {separator}, StringSplitOptions.None).ToList();
        }

        public static string ToUpperInitial(this string value)
        {
            switch (value)
            {
                case null: throw new ArgumentNullException(nameof(value));
                case "": throw new ArgumentException($"{nameof(value)} cannot be empty", nameof(value));
                default: return value.First().ToString().ToUpper() + value.Substring(1);
            }
        }

        public static string ToLowerInitial(this string value)
        {
            switch (value)
            {
                case null: throw new ArgumentNullException(nameof(value));
                case "": throw new ArgumentException($"{nameof(value)} cannot be empty", nameof(value));
                default: return value.First().ToString().ToLower() + value.Substring(1);
            }
        }
    }
}

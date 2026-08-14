using System;
using System.Text;

namespace TestGame001
{
    // Generates fantasy-sounding monster names (first name + surname) by combining short
    // syllable fragments. No external word list involved - the fragments are just letter
    // combinations, so there's no copyright concern.
    public static class MonsterNameGenerator
    {
        private static readonly string[] Prefixes =
        {
            "Gor", "Thal", "Vex", "Mor", "Kra", "Zul", "Bel", "Ash", "Drak", "Grim",
            "Nyx", "Skar", "Vor", "Uld", "Krag", "Thok", "Zar", "Mal", "Rok", "Bru"
        };

        private static readonly string[] Middles =
        {
            "ul", "ath", "or", "en", "ir", "ok", "ash", "und", "arn", "esh",
            "ol", "ux", "in", "az", "oth", "yor", "el", "um", "irn"
        };

        private static readonly string[] Suffixes =
        {
            "tharn", "gor", "mok", "reth", "vash", "nir", "dax", "grim", "zor", "keth",
            "wyn", "duk", "rath", "los", "grath"
        };

        private static readonly Random random = new Random();

        // Builds one name part (first name or surname) by combining a prefix, an optional
        // middle fragment (skipped about 1 in 3 times), and a suffix.
        private static string GenerateNamePart()
        {
            var sb = new StringBuilder();
            sb.Append(Prefixes[random.Next(Prefixes.Length)]);

            if (random.Next(3) != 0)
            {
                sb.Append(Middles[random.Next(Middles.Length)]);
            }

            sb.Append(Suffixes[random.Next(Suffixes.Length)]);

            return sb.ToString();
        }

        // Returns a full "Firstname Surname" style name, e.g. "Gorul Ashtharn".
        public static string Generate()
        {
            string firstName = GenerateNamePart();
            string surname = GenerateNamePart();
            return firstName + " " + surname;
        }
    }
}
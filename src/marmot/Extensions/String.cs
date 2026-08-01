using System.Globalization;
using static System.Text.RegularExpressions.Regex;

namespace Marmot;

public static partial class Extensions {

    extension(string input) {

        public string ToSafer()
            => Replace(input.ToLowerInvariant().Replace(' ', '-'), @"[^a-zA-Z0-9\-_]", "");

        public string ToPascalCase() {

            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(

                Replace(input, @"^\d+", "")
                    .Replace('-', ' ')
                    .Replace('_', ' ')
                    .ToLower()

            ).Replace(" ", string.Empty);
        }
    }
}
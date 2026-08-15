using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

var knownPlaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "hagia sophia grand mosque",
    "the blue mosque",
    "sultanahmet square",
    "dolmabahce palace",
    "suleymaniye mosque",
    "clock tower of izmir",
    "izmir historical elevator building",
    "kizlaragasi inn"
};

string NormalizePlaceName(string name)
{
    if (string.IsNullOrWhiteSpace(name)) return string.Empty;
    var normalized = name.Trim().ToLowerInvariant();
    normalized = Regex.Replace(normalized, @"[^\w\s]", ""); // Remove punctuation
    return normalized;
}

var lines = new[]
{
    "- Hagia Sophia",
    "- **Morning:** Visit **Hagia Sophia**",
    "- Visit the Blue Mosque",
    "- Sultanahmet Square",
    "- Explore Dolmabahçe Palace",
    "  - Suleymaniye Mosque",
    "- Visit **Hagia Sophia Grand Mosque**"
};

string VerifyPlacesInMarkdown(string markdown)
{
    var linesArr = markdown.Split('\n');
    for (int i = 0; i < linesArr.Length; i++)
    {
        var line = linesArr[i];
        var trimmedLine = line.Trim();
        
        if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* ") || Regex.IsMatch(trimmedLine, @"^\d+\.\s"))
        {
            var contentWithoutBullets = Regex.Replace(trimmedLine, @"^[-*]\s*", "");
            contentWithoutBullets = Regex.Replace(contentWithoutBullets, @"^\d+\.\s*", "");

            var potentialPlaces = new List<string>();
            
            // 1. Full content without bullets and bold markers
            potentialPlaces.Add(Regex.Replace(contentWithoutBullets, @"\*\*(.*?)\*\*", "$1"));

            // 2. Extracted bold text
            var boldMatches = Regex.Matches(contentWithoutBullets, @"\*\*(.*?)\*\*");
            foreach (Match m in boldMatches)
            {
                potentialPlaces.Add(m.Groups[1].Value);
            }

            // 3. Content without common verbs
            var withoutVerbs = Regex.Replace(contentWithoutBullets, @"\b(visit|explore|head to|see|go to|discover|walk through|tour|enjoy|experience)\b", "", RegexOptions.IgnoreCase);
            withoutVerbs = Regex.Replace(withoutVerbs, @"\b(the|a|an)\b", "", RegexOptions.IgnoreCase);
            potentialPlaces.Add(Regex.Replace(withoutVerbs, @"\*\*(.*?)\*\*", "$1"));

            bool verified = false;
            foreach (var potentialPlace in potentialPlaces)
            {
                var normalizedPlace = NormalizePlaceName(potentialPlace);
                if (normalizedPlace.Length < 4) continue;

                if (knownPlaces.Any(kp => 
                    normalizedPlace == kp || 
                    (normalizedPlace.Contains(kp) && kp.Length > 4) ||
                    (kp.Contains(normalizedPlace) && normalizedPlace.Length > 4)))
                {
                    verified = true;
                    break;
                }
            }

            if (verified && !line.Contains("Verified by community"))
            {
                linesArr[i] = line.TrimEnd('\r') + " *(✓ Verified by community)*\r";
            }
        }
    }
    return string.Join("\n", linesArr);
}

foreach (var line in lines)
{
    Console.WriteLine(VerifyPlacesInMarkdown(line));
}

using System;
using System.Text.RegularExpressions;

string ExtractDestinationFast(string prompt)
{
    var extracted = Regex.Replace(prompt, @"(?i)^(i\s+want\s+to\s+(?:travel|go|visit)\s+to\s+|a\s+\d+-day\s+trip\s+to\s+|trip\s+to\s+|travel\s+to\s+|visit\s+|vacation\s+in\s+)", "").Trim();
    extracted = Regex.Replace(extracted, @"(?i)^\d+\s+days?\s+in\s+", "").Trim();
    extracted = Regex.Replace(extracted, @"[.?!]+$", "").Trim();
    
    // Quick normalization for common known aliases if we don't want to hit the DB with a complex query
    if (extracted.Equals("Turkey", StringComparison.OrdinalIgnoreCase)) 
    {
        // Wait, the user said "بدون hardcoding خاص بهذه الحالة فقط"
        // So no hardcoding Turkey -> Türkiye.
    }
    
    return extracted;
}

Console.WriteLine(ExtractDestinationFast(""I want to travel to Türkiye""));
Console.WriteLine(ExtractDestinationFast(""A 5-day trip to Türkiye""));
Console.WriteLine(ExtractDestinationFast(""I want to travel to France""));
Console.WriteLine(ExtractDestinationFast(""5 days in Istanbul""));

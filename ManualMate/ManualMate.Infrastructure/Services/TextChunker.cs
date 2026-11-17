using System.Text;
using System.Text.RegularExpressions;
using ManualMate.Application.Interfaces.Services;
using ManualMate.Application.Responses;

namespace ManualMate.Infrastructure.Services
{
    public class TextChunker : ITextChunker
    {
        // Split only after ., !, ? when followed by whitespace and a capital letter or a quote,
        // so "v2.1", "Fig. 3" and numbered items like "step 2" don't cause splits.
        private static readonly Regex SentenceSplitRegex = new(@"(?<=[.!?])\s+(?=[\p{Lu}""'])", RegexOptions.Compiled);

        // "3." at the end of a fragment = numbered list item, merge it back with its text.
        private static readonly Regex ListItemEndRegex = new(@"\b\d{1,3}\.$", RegexOptions.Compiled);

        // "J." at the end of a fragment = person initial, merge it with the next word.
        private static readonly Regex InitialEndRegex = new(@"\b\p{Lu}\.$", RegexOptions.Compiled);

        // Last word before the final dot, used for the abbreviation lookup below.
        private static readonly Regex LastWordRegex = new(@"(\p{L}[\w.]*)\.$", RegexOptions.Compiled);

        private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
        {
            "e.g", "i.e", "etc", "fig", "no", "vs", "approx", "cf", "sec", "ref", "vol", "ch", "pg"
        };

        public Result<List<string>> ChunkText(string text, int maxChars = 1000, int overlapChars = 150)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return Result<List<string>>.Ok(new List<string>());

                var sentences = SplitIntoSentences(text);

                var chunks = new List<string>();
                var seedText = string.Empty; // tail of the previous chunk, carried into the next one
                var content = new StringBuilder();
                var overlap = new Queue<string>(); // sentences kept as the next chunk's seed
                var overlapLength = 0;

                foreach (var rawSentence in sentences)
                {
                    foreach (var sentence in HardSplit(rawSentence, maxChars))
                    {
                        if (content.Length > 0 && content.Length + 1 + sentence.Length > maxChars)
                        {
                            chunks.Add(string.Concat(seedText, content).Trim());
                            seedText = overlap.Count > 0 ? string.Join(" ", overlap) + " " : string.Empty;
                            content.Clear();
                        }

                        content.Append(sentence).Append(' ');

                        overlap.Enqueue(sentence);
                        overlapLength += sentence.Length + 1;
                        while (overlapLength > overlapChars && overlap.Count > 0)
                            overlapLength -= overlap.Dequeue().Length + 1;
                    }
                }

                if (content.Length > 0)
                    chunks.Add(string.Concat(seedText, content).Trim());

                return Result<List<string>>.Ok(chunks);
            }
            catch (Exception ex)
            {
                return Result<List<string>>.Fail("Error chunking text: " + ex.Message);
            }
        }

        private static List<string> SplitIntoSentences(string text)
        {
            var sentences = new List<string>();

            foreach (var fragment in SentenceSplitRegex.Split(text))
            {
                var trimmed = fragment.Trim();
                if (trimmed.Length == 0)
                    continue;

                // Fix splits after "1.", "J." and abbreviations like "e.g." by rejoining fragments.
                if (sentences.Count > 0 && IsBadSplit(sentences[^1]))
                    sentences[^1] += " " + trimmed;
                else
                    sentences.Add(trimmed);
            }

            return sentences;
        }

        private static bool IsBadSplit(string fragment)
        {
            if (ListItemEndRegex.IsMatch(fragment) || InitialEndRegex.IsMatch(fragment))
                return true;

            var lastWord = LastWordRegex.Match(fragment);
            return lastWord.Success && Abbreviations.Contains(lastWord.Groups[1].Value);
        }

        // A single sentence longer than maxChars is cut at the last space below the limit.
        private static IEnumerable<string> HardSplit(string sentence, int maxChars)
        {
            var remaining = sentence;

            while (remaining.Length > maxChars)
            {
                var cut = remaining.LastIndexOf(' ', maxChars - 1);
                if (cut <= 0)
                    cut = maxChars; // no space nearby: cut mid-word

                yield return remaining[..cut].Trim();
                remaining = remaining[cut..].TrimStart();
            }

            if (remaining.Length > 0)
                yield return remaining;
        }
    }
}

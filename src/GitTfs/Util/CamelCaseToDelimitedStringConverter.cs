namespace GitTfs.Util
{
    public static class CamelCaseToDelimitedStringConverter
    {
        public static string Convert(string stringWithCamelCase, string delimiter)
        {
            var words = stringWithCamelCase
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(GetCaseDelimitedParts).Select(p => p.ToLowerInvariant());

            return string.Join(delimiter, words);
        }

        private static IEnumerable<string> GetCaseDelimitedParts(string s)
        {
            var wordStartIndex = 0;
            for (var index = 0; index < s.Length; index++)
            {
                if (IsFirstCharacterOfNewWord(s, index, wordStartIndex))
                {
                    yield return s.Substring(wordStartIndex, index - wordStartIndex).ToLowerInvariant();
                    wordStartIndex = index;
                }
            }

            yield return s.Substring(wordStartIndex).ToLowerInvariant();
        }

        private static bool IsFirstCharacterOfNewWord(string s, int index, int wordStartIndex)
        {
            if (index == 0)
                return false;

            return char.IsUpper(s[index]) && !char.IsUpper(s[index - 1]) ||
                   char.IsUpper(s[index]) && char.IsUpper(s[index - 1]) && wordStartIndex == index - 2 ||
                   char.IsUpper(s[index]) && char.IsUpper(s[index - 1]) && s.Length >= index + 2 && !char.IsUpper(s[index + 1]);
        }
    }
}
namespace Emqo.NoNameTag.Utilities
{
    /// <summary>
    /// Sanitizes untrusted player text before it is inserted into Unity rich text messages.
    /// Server-authored configuration text remains trusted and is not processed here.
    /// </summary>
    public static class RichTextSanitizer
    {
        public static string SanitizeUntrustedPlayerText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            System.Text.StringBuilder builder = null;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var isBlocked = c == '<' || c == '>' || c == '{' || c == '}';
                if (!isBlocked)
                {
                    builder?.Append(c);
                    continue;
                }

                if (builder == null)
                {
                    builder = new System.Text.StringBuilder(text.Length);
                    if (i > 0)
                        builder.Append(text, 0, i);
                }
            }

            return builder == null ? text : builder.ToString();
        }
    }
}

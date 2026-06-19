using Emqo.NoNameTag.Utilities;
using Xunit;

namespace Emqo.NoNameTag.Tests
{
    public sealed class RichTextSanitizerTests
    {
        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("<color=red>owned</color>", "color=redowned/color")]
        [InlineData("{server_icon}<size=20>x</size>", "server_iconsize=20x/size")]
        public void SanitizeUntrustedPlayerText_RemovesRichTextControlCharacters(string input, string expected)
        {
            Assert.Equal(expected, RichTextSanitizer.SanitizeUntrustedPlayerText(input));
        }

        [Fact]
        public void SanitizeUntrustedPlayerText_TreatsNullAsEmpty()
        {
            Assert.Equal(string.Empty, RichTextSanitizer.SanitizeUntrustedPlayerText(null));
        }
    }
}

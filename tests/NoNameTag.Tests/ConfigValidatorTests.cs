using Emqo.NoNameTag.Utilities;
using Xunit;

namespace Emqo.NoNameTag.Tests
{
    public sealed class ConfigValidatorTests
    {
        [Theory]
        [InlineData("#FFFFFF")]
        [InlineData("FFFFFF")]
        [InlineData("white")]
        [InlineData("red")]
        [InlineData("yellow")]
        public void ValidateColorValue_AcceptsHexAndDocumentedUnityColorNames(string color)
        {
            Assert.True(ConfigValidator.ValidateColorValue(color, out var error), error);
        }

        [Theory]
        [InlineData("#FFFFF")]
        [InlineData("not-a-color")]
        [InlineData("orange")]
        [InlineData("purple")]
        [InlineData("rgb(255,255,255)")]
        public void ValidateColorValue_RejectsUnsupportedColors(string color)
        {
            Assert.False(ConfigValidator.ValidateColorValue(color, out _));
        }
    }
}

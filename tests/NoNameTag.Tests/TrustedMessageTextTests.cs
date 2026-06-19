using Emqo.NoNameTag.Utilities;
using Xunit;

namespace Emqo.NoNameTag.Tests
{
    public sealed class TrustedMessageTextTests
    {
        [Fact]
        public void WrapWithStyle_PreservesExistingTrustedRichText()
        {
            var message = "<b>Server</b> says hello";

            var formatted = NameFormatter.WrapWithStyle(message, "red", 0);

            Assert.Contains("<b>Server</b>", formatted);
            Assert.Contains("<color=red>", formatted);
        }
    }
}

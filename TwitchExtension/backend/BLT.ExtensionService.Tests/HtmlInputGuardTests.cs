using System.Text.Json;

namespace BLT.ExtensionService.Tests;

public sealed class HtmlInputGuardTests
{
    [Theory]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("&lt;script&gt;alert(1)&lt;/script&gt;")]
    [InlineData("Steel > Iron")]
    public void ContainsHtmlRejectsMarkupLikeText(string value)
    {
        Assert.True(HtmlInputGuard.ContainsHtml(value));
    }

    [Theory]
    [InlineData("Foehammer")]
    [InlineData("Blade of Dawn")]
    [InlineData("Axe #1")]
    public void ContainsHtmlAllowsPlainNames(string value)
    {
        Assert.False(HtmlInputGuard.ContainsHtml(value));
    }

    [Fact]
    public void ContainsHtmlRejectsNestedActionArguments()
    {
        using var document = JsonDocument.Parse("""{"name":"ok","nested":{"caption":"<b>bad</b>"}}""");
        var args = new Dictionary<string, JsonElement> { ["payload"] = document.RootElement.Clone() };

        Assert.True(HtmlInputGuard.ContainsHtml(args));
    }
}

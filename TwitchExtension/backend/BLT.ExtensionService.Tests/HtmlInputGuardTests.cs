using System.Text.Json;

namespace BLT.ExtensionService.Tests;

public sealed class HtmlInputGuardTests
{
    [Theory]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("&lt;script&gt;alert(1)&lt;/script&gt;")]
    [InlineData("Steel > Iron")]
    [InlineData("!clan rename <img src=x onerror=\"console.log('BLT_TEST')\">")]
    [InlineData("!kingdom create &LT;b&GT;test&LT;/b&GT;")]
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

    [Theory]
    [InlineData("{\"name\":[\"ok\",\"<img src=x onerror=console.log(1)>\"]}")]
    [InlineData("{\"<b>key</b>\":\"ok\"}")]
    [InlineData("{\"name\":\"\\u003cimg src=x\\u003e\"}")]
    public void ContainsHtmlRejectsArraysKeysAndJsonEscapedMarkup(string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.True(HtmlInputGuard.ContainsHtml(document.RootElement));
    }
}

using FluentAssertions;
using Xunit.Abstractions;
using static ExtractUrls.LinkUtils;

namespace ExtractUrls.Tests;

public class LinkUtilsTests : XunitContextBase
{
    [Fact] public void TrimTitle_Should_Trim_And_Remove_Line_Breaks() => 
        TrimTitle("   Hello, \rWorld\n!  ")
            .Should().Be("Hello,  World !");

    [Fact] public void TrimTitle_Should_Return_Null_When_Input_Is_Null_Or_WhiteSpace() => 
        TrimTitle(null)
            .Should().BeNull();

    

    public LinkUtilsTests(ITestOutputHelper logger) : base(logger) { }
}
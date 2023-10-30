namespace altinn_app_cli.Tests.v7Tov8.CodeRewriters;

using altinn_app_cli.v7Tov8.CodeRewriters;
using Microsoft.CodeAnalysis.CSharp;
public class ReturnTypeRewriterTests
{
    [Fact]
    public async Task VerifyReturnTypeUpdate()
    {
        var preUpdate = """
        namespace Test;

        public class TestMyReturnUpdater : IReturnUpdaterTest
        {
            public async Task<string> GetString()
            {
                if(true)
                {
                    return null;
                }
                return "my String"
            }
            public Task<string> NotUpdated()
            {
                return Task.FromResult("dd");
            }
        }
        """;
        var postUpdate = """
        namespace Test;

        public class TestMyReturnUpdater : IReturnUpdaterTest
        {
            public async Task<string?> GetString()
            {
                if(true)
                {
                    return null;
                }
                return "my String"
            }
            public Task<string> NotUpdated()
            {
                return Task.FromResult("dd");
            }
        }
        """;

        var returnTypeRewriter = new ReturnTypeRewriter("IReturnUpdaterTest", "GetString", "Task<string>", "Task<string?>");
        var parsedPre = CSharpSyntaxTree.ParseText(preUpdate);
        var updatedNode = returnTypeRewriter.Visit(await parsedPre.GetRootAsync());

        updatedNode.ToFullString().Should().Be(postUpdate);




    }
}
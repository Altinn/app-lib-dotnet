namespace altinn_app_cli.Tests.v7Tov8.CodeRewriters;

using altinn_app_cli.v7Tov8.CodeRewriters;
using Microsoft.CodeAnalysis.CSharp;
public class IDataProcessorRewriterTests
{
    [Fact]
    public async Task VerifyReturnStatementUpdate()
    {
        // Different possible return statements to verify that everything is correctly transformed 
        var preUpdate = """
        public class DataProcessor : IDataProcessor
        {
            // Async method
            public async Task<bool> ProcessDataWrite(Instance instance, Guid dataGuid, object data)
            {
                var changed = true;
                if (changed)
                {
                    return true;
                }

                return await Task.FromResult(changed);
            }
            // Non async method
            public Task<bool> ProcessDataRead(Instance instance, Guid dataGuid, object data)
            {
                if("a" == "b")
                {
                    return Task.FromResult(a+b);
                }

                return Task.FromResult(true);
            }
        }
        """;
        var postUpdate = """
        public class DataProcessor : IDataProcessor
        {
            // Async method
            public async Task ProcessDataWrite(Instance instance, Guid dataGuid, object data, Dictionary<string, string?>? changedFields)
            {
                var changed = true;
                if (changed)
                {
                    return;
                }

                return;
            }
            // Non async method
            public Task ProcessDataRead(Instance instance, Guid dataGuid, object data)
            {
                if("a" == "b")
                {
                    a+b;
                    return Task.CompletedTask;
                }

                return Task.CompletedTask;
            }
        }
        """;

        var processorRewriter = new IDataProcessorRewriter();
        var parsedPre = CSharpSyntaxTree.ParseText(preUpdate);
        var updatedNode = processorRewriter.Visit(await parsedPre.GetRootAsync());

        updatedNode.ToFullString().Should().Be(postUpdate);
    }
}
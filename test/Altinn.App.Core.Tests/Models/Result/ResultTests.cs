using Altinn.App.Core.Models.Result;
using FluentAssertions;
using Xunit;

namespace Altinn.App.Core.Tests.Models.Result;

public class ResultTests
{
    [Fact]
    public void Unwrap_returns_value_when_value_not_null()
    {
        // Arrange
        var res = Result<string, DummyError>.Ok("value");
        res.Unwrap().Should().BeEquivalentTo("value");
    }
    
    [Fact]
    public void Unwrap_throws_exception_when_value_null()
    {
        // Arrange
        var res = Result<string, DummyError>.Err(new DummyError());
        res.Invoking(r => r.Unwrap()).Should().Throw<Exception>().WithMessage("Dummy error reason");
    }
    
    [Fact]
    public void UnwrapOrElse_returns_value_when_value_not_null()
    {
        // Arrange
        var res = Result<string, DummyError>.Ok("value");
        res.UnwrapOrElse(e => "error").Should().BeEquivalentTo("value");
    }
    
    [Fact]
    public void UnwrapOrElse_returns_error_when_value_null()
    {
        // Arrange
        var res = Result<string, DummyError>.Err(new DummyError());
        res.UnwrapOrElse(e => e.Reason()).Should().BeEquivalentTo("Dummy error reason");
    }
    
    [Fact]
    public void Map_execute_ok_function_when_value_not_null()
    {
        // Arrange
        var res = Result<string, DummyError>.Ok("value");
        res.Map(s => s.Length, e => e.Reason().Length).Should().Be(5);
    }
    
    [Fact]
    public void Map_execute_error_function_when_value_null()
    {
        // Arrange
        var res = Result<string, DummyError>.Err(new DummyError());
        res.Map(s => s.Length, e => e.Reason().Length).Should().Be(18);
    }
    
    [Fact]
    public void Converts_to_ResultType_Ok_when_value_not_null()
    {
        // Arrange
        var res = Result<string, DummyError>.Ok("value");
        ((ResultType)res).Should().Be(ResultType.Ok);
    }
    
    [Fact]
    public void Converts_to_ResultType_Ok_when_value_null()
    {
        // Arrange
        var res = Result<string, DummyError>.Err(new DummyError());
        ((ResultType)res).Should().Be(ResultType.Err);
    }
    
    [Fact]
    public void Value_returns_value()
    {
        // Arrange
        var value = "value";
        var res = Result<string, DummyError>.Ok(value);
        res.Value().Should().Be(value);
    }
    
    [Fact]
    public void Error_returns_error()
    {
        // Arrange
        var dummyError = new DummyError();
        var res = Result<string, DummyError>.Err(dummyError);
        res.Error().Should().Be(dummyError);
    }
    
    private class DummyError : IResultError
    {
        public string Reason()
        {
            return "Dummy error reason";
        }
    }
}

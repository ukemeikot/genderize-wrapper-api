using FluentAssertions;
using GenderClassifyApi.Validators;
using Microsoft.AspNetCore.Http;

namespace GenderClassifyApi.Tests.Validators;

public sealed class NameParameterValidatorTests
{
    private readonly NameParameterValidator _validator = new();

    [Fact]
    public void Validate_ShouldReturnBadRequest_WhenNameIsMissing()
    {
        var request = new DefaultHttpContext().Request;

        var result = _validator.Validate(request, null);

        result.IsValid.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.ErrorMessage.Should().Be("Name parameter is required");
    }

    [Fact]
    public void Validate_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString("?name=");

        var result = _validator.Validate(request, string.Empty);

        result.IsValid.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.ErrorMessage.Should().Be("Name parameter is required");
    }

    [Fact]
    public void Validate_ShouldReturnUnprocessableEntity_WhenNameIsDuplicated()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?name=James&name=John");

        var result = _validator.Validate(context.Request, "James");

        result.IsValid.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        result.ErrorMessage.Should().Be("Name must be a valid string");
    }

    [Fact]
    public void Validate_ShouldReturnUnprocessableEntity_WhenNameLooksLikeAnArray()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?name[]=James");

        var result = _validator.Validate(context.Request, "James");

        result.IsValid.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        result.ErrorMessage.Should().Be("Name must be a valid string");
    }

    [Fact]
    public void Validate_ShouldReturnTrimmedName_WhenRequestIsValid()
    {
        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString("?name=%20James%20");

        var result = _validator.Validate(request, " James ");

        result.IsValid.Should().BeTrue();
        result.Name.Should().Be("James");
    }
}

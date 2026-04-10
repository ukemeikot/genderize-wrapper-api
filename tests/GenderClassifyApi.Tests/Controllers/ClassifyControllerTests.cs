using FluentAssertions;
using GenderClassifyApi.Controllers;
using GenderClassifyApi.Models;
using GenderClassifyApi.Services;
using GenderClassifyApi.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GenderClassifyApi.Tests.Controllers;

public sealed class ClassifyControllerTests
{
    private readonly Mock<IGenderizeService> _genderizeService = new();
    private readonly NameParameterValidator _validator = new();

    [Fact]
    public async Task Classify_ShouldReturnBadRequest_WhenNameIsMissing()
    {
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Classify(null, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        objectResult.Value.Should().BeEquivalentTo(new ErrorResponse("Name parameter is required"));
    }

    [Fact]
    public async Task Classify_ShouldReturnUnprocessableEntity_WhenNameIsArrayLike()
    {
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.HttpContext.Request.QueryString = new QueryString("?name[]=James");

        var result = await controller.Classify(null, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        objectResult.Value.Should().BeEquivalentTo(new ErrorResponse("Name must be a valid string"));
    }

    [Fact]
    public async Task Classify_ShouldReturnNotFound_WhenGenderizeHasNoPrediction()
    {
        _genderizeService
            .Setup(service => service.GetGenderPredictionAsync("Xqzptlw", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenderizeApiResponse
            {
                Name = "Xqzptlw",
                Gender = null,
                Probability = 0,
                Count = 0
            });

        var controller = CreateController("?name=Xqzptlw");

        var result = await controller.Classify("Xqzptlw", CancellationToken.None);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(
            new ErrorResponse("No prediction available for the provided name"));
    }

    [Fact]
    public async Task Classify_ShouldReturnNotFound_WhenGenderizeReturnsZeroSampleSize()
    {
        _genderizeService
            .Setup(service => service.GetGenderPredictionAsync("RareName", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenderizeApiResponse
            {
                Name = "RareName",
                Gender = "male",
                Probability = 0.85,
                Count = 0
            });

        var controller = CreateController("?name=RareName");

        var result = await controller.Classify("RareName", CancellationToken.None);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(
            new ErrorResponse("No prediction available for the provided name"));
    }

    [Fact]
    public async Task Classify_ShouldReturnSuccessResponse_WhenPredictionExists()
    {
        _genderizeService
            .Setup(service => service.GetGenderPredictionAsync("James", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenderizeApiResponse
            {
                Name = "James",
                Gender = "male",
                Probability = 0.99,
                Count = 1234
            });

        var controller = CreateController("?name=James");

        var result = await controller.Classify("James", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = okResult.Value.Should().BeOfType<ClassifyResponse>().Subject;
        payload.Status.Should().Be("success");
        payload.Data.Name.Should().Be("James");
        payload.Data.Gender.Should().Be("male");
        payload.Data.Probability.Should().Be(0.99);
        payload.Data.SampleSize.Should().Be(1234);
        payload.Data.IsConfident.Should().BeTrue();
        payload.Data.ProcessedAt.Should().EndWith("Z");
    }

    [Fact]
    public async Task Classify_ShouldSetIsConfidentToTrue_WhenValuesAreExactlyAtThreshold()
    {
        _genderizeService
            .Setup(service => service.GetGenderPredictionAsync("Jordan", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenderizeApiResponse
            {
                Name = "Jordan",
                Gender = "male",
                Probability = 0.7,
                Count = 100
            });

        var controller = CreateController("?name=Jordan");

        var result = await controller.Classify("Jordan", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = okResult.Value.Should().BeOfType<ClassifyResponse>().Subject;
        payload.Data.IsConfident.Should().BeTrue();
    }

    [Fact]
    public async Task Classify_ShouldSetIsConfidentToFalse_WhenProbabilityOrSampleSizeAreBelowThreshold()
    {
        _genderizeService
            .Setup(service => service.GetGenderPredictionAsync("Taylor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenderizeApiResponse
            {
                Name = "Taylor",
                Gender = "female",
                Probability = 0.7,
                Count = 99
            });

        var controller = CreateController("?name=Taylor");

        var result = await controller.Classify("Taylor", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = okResult.Value.Should().BeOfType<ClassifyResponse>().Subject;
        payload.Data.IsConfident.Should().BeFalse();
    }

    [Fact]
    public async Task Classify_ShouldSetIsConfidentToFalse_WhenProbabilityIsBelowThreshold()
    {
        _genderizeService
            .Setup(service => service.GetGenderPredictionAsync("Alex", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenderizeApiResponse
            {
                Name = "Alex",
                Gender = "male",
                Probability = 0.69,
                Count = 200
            });

        var controller = CreateController("?name=Alex");

        var result = await controller.Classify("Alex", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = okResult.Value.Should().BeOfType<ClassifyResponse>().Subject;
        payload.Data.IsConfident.Should().BeFalse();
    }

    private ClassifyController CreateController(string? queryString = null)
    {
        var controller = new ClassifyController(_genderizeService.Object, _validator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (!string.IsNullOrWhiteSpace(queryString))
        {
            controller.HttpContext.Request.QueryString = new QueryString(queryString);
        }

        return controller;
    }
}

using System.Net;
using System.Text;
using FluentAssertions;
using GenderClassifyApi.Models;
using GenderClassifyApi.Services;
using Microsoft.Extensions.Options;

namespace GenderClassifyApi.Tests.Services;

public sealed class GenderizeServiceTests
{
    [Fact]
    public async Task GetGenderPredictionAsync_ShouldReturnPayload_WhenGenderizeRespondsSuccessfully()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"name":"James","gender":"male","probability":0.99,"count":1234}
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.genderize.io/")
        };

        var service = CreateService(httpClient);

        var result = await service.GetGenderPredictionAsync("James");

        result.Should().BeEquivalentTo(new GenderizeApiResponse
        {
            Name = "James",
            Gender = "male",
            Probability = 0.99,
            Count = 1234
        });
    }

    [Fact]
    public async Task GetGenderPredictionAsync_ShouldIncludeApiKey_WhenConfigured()
    {
        Uri? capturedRequestUri = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequestUri = request.RequestUri;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"name":"James","gender":"male","probability":0.99,"count":1234}
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.genderize.io/")
        };

        var service = CreateService(httpClient, apiKey: "secret-key");

        await service.GetGenderPredictionAsync("James");

        capturedRequestUri.Should().NotBeNull();
        capturedRequestUri!.Query.Should().Contain("apikey=secret-key");
        capturedRequestUri.Query.Should().Contain("name=James");
    }

    [Fact]
    public async Task GetGenderPredictionAsync_ShouldThrow_WhenGenderizeReturnsNonSuccessStatusCode()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.genderize.io/")
        };

        var service = CreateService(httpClient);

        var action = async () => await service.GetGenderPredictionAsync("James");

        await action.Should().ThrowAsync<GenderizeUnavailableException>()
            .WithMessage("Unable to reach the gender prediction service");
    }

    [Fact]
    public async Task GetGenderPredictionAsync_ShouldThrow_WhenHttpRequestFails()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("network error"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.genderize.io/")
        };

        var service = CreateService(httpClient);

        var action = async () => await service.GetGenderPredictionAsync("James");

        await action.Should().ThrowAsync<GenderizeUnavailableException>()
            .WithMessage("Unable to reach the gender prediction service");
    }

    [Fact]
    public async Task GetGenderPredictionAsync_ShouldThrow_WhenRequestTimesOut()
    {
        var handler = new TimeoutHttpMessageHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.genderize.io/"),
            Timeout = TimeSpan.FromMilliseconds(50)
        };

        var service = CreateService(httpClient);

        var action = async () => await service.GetGenderPredictionAsync("James");

        await action.Should().ThrowAsync<GenderizeUnavailableException>()
            .WithMessage("Unable to reach the gender prediction service");
    }

    private static GenderizeService CreateService(HttpClient httpClient, string? apiKey = null)
    {
        return new GenderizeService(
            httpClient,
            Options.Create(new GenderizeOptions
            {
                BaseUrl = "https://api.genderize.io/",
                TimeoutSeconds = 3,
                ApiKey = apiKey
            }));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    private sealed class TimeoutHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}

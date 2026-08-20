using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Exceptions;
using PartnerIntegration.Infrastructure.PartnerVerification;

namespace PartnerIntegration.UnitTests.PartnerVerification;

public sealed class ResilientPartnerVerificationClientTests
{
    [Fact]
    public async Task Succeeds_without_retry_when_inner_call_succeeds()
    {
        var inner = Substitute.For<IPartnerVerificationClient>();
        inner.VerifyAsync("P-1001", Arg.Any<CancellationToken>())
            .Returns(new PartnerVerificationResponse("P-1001", true, "Acme"));

        var sut = CreateSut(inner, maxRetries: 3);

        var result = await sut.VerifyAsync("P-1001", CancellationToken.None);

        Assert.True(result.IsVerified);
        await inner.Received(1).VerifyAsync("P-1001", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retries_on_TimeoutException_and_returns_successful_response()
    {
        var inner = Substitute.For<IPartnerVerificationClient>();
        inner.VerifyAsync("P-1001", Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new TimeoutException("boom"),
                _ => throw new TimeoutException("boom"),
                _ => new PartnerVerificationResponse("P-1001", true, "Acme"));

        var sut = CreateSut(inner, maxRetries: 3);

        var result = await sut.VerifyAsync("P-1001", CancellationToken.None);

        Assert.True(result.IsVerified);
        await inner.Received(3).VerifyAsync("P-1001", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retries_on_HttpRequestException()
    {
        var inner = Substitute.For<IPartnerVerificationClient>();
        inner.VerifyAsync("P-1001", Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new HttpRequestException("connection reset"),
                _ => new PartnerVerificationResponse("P-1001", true, "Acme"));

        var sut = CreateSut(inner, maxRetries: 2);

        var result = await sut.VerifyAsync("P-1001", CancellationToken.None);

        Assert.True(result.IsVerified);
        await inner.Received(2).VerifyAsync("P-1001", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_unavailable_exception_when_retries_are_exhausted()
    {
        var inner = Substitute.For<IPartnerVerificationClient>();
        inner.VerifyAsync("P-1001", Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("always down"));

        var sut = CreateSut(inner, maxRetries: 2);

        var exception = await Assert.ThrowsAsync<PartnerVerificationUnavailableException>(
            () => sut.VerifyAsync("P-1001", CancellationToken.None));

        Assert.Contains("P-1001", exception.Message);
        await inner.Received(3).VerifyAsync("P-1001", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_retry_unexpected_exceptions()
    {
        var inner = Substitute.For<IPartnerVerificationClient>();
        inner.VerifyAsync("P-1001", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("bug"));

        var sut = CreateSut(inner, maxRetries: 3);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.VerifyAsync("P-1001", CancellationToken.None));

        await inner.Received(1).VerifyAsync("P-1001", Arg.Any<CancellationToken>());
    }

    private static ResilientPartnerVerificationClient CreateSut(IPartnerVerificationClient inner, int maxRetries)
    {
        var pipeline = PartnerVerificationResilience.CreatePipeline(new PartnerVerificationOptions
        {
            MaxRetryAttempts = maxRetries,
            InitialRetryDelayMilliseconds = 0
        });

        return new ResilientPartnerVerificationClient(inner, pipeline, NullLogger<ResilientPartnerVerificationClient>.Instance);
    }
}

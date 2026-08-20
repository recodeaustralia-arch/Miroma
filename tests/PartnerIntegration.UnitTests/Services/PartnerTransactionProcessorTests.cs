using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Exceptions;
using PartnerIntegration.Application.Services;

namespace PartnerIntegration.UnitTests.Services;

public sealed class PartnerTransactionProcessorTests
{
    [Fact]
    public async Task Queues_transaction_when_partner_is_verified()
    {
        var verifier = Substitute.For<IPartnerVerificationClient>();
        var publisher = Substitute.For<ITransactionQueuePublisher>();
        verifier.VerifyAsync("P-1001", Arg.Any<CancellationToken>())
            .Returns(new PartnerVerificationResponse("P-1001", true, "Acme"));

        var processor = new PartnerTransactionProcessor(verifier, publisher, NullLogger<PartnerTransactionProcessor>.Instance);

        var result = await processor.ProcessAsync(ValidRequest(), CancellationToken.None);

        Assert.Equal(ProcessTransactionStatus.Accepted, result.Status);
        Assert.Equal("TXN-99823", result.TransactionReference);
        await publisher.Received(1).PublishAsync(
            Arg.Is<PartnerTransactionMessage>(m =>
                m.PartnerId == "P-1001"
                && m.TransactionReference == "TXN-99823"
                && m.Amount == 250.00m
                && m.Currency == "USD"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_queue_when_partner_is_not_verified()
    {
        var verifier = Substitute.For<IPartnerVerificationClient>();
        var publisher = Substitute.For<ITransactionQueuePublisher>();
        verifier.VerifyAsync("P-9999", Arg.Any<CancellationToken>())
            .Returns(new PartnerVerificationResponse("P-9999", false, null));

        var processor = new PartnerTransactionProcessor(verifier, publisher, NullLogger<PartnerTransactionProcessor>.Instance);
        var request = ValidRequest();
        request.PartnerId = "P-9999";

        var result = await processor.ProcessAsync(request, CancellationToken.None);

        Assert.Equal(ProcessTransactionStatus.PartnerNotVerified, result.Status);
        await publisher.DidNotReceive().PublishAsync(Arg.Any<PartnerTransactionMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_unavailable_when_verification_retries_are_exhausted()
    {
        var verifier = Substitute.For<IPartnerVerificationClient>();
        var publisher = Substitute.For<ITransactionQueuePublisher>();
        verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new PartnerVerificationUnavailableException("down"));

        var processor = new PartnerTransactionProcessor(verifier, publisher, NullLogger<PartnerTransactionProcessor>.Instance);

        var result = await processor.ProcessAsync(ValidRequest(), CancellationToken.None);

        Assert.Equal(ProcessTransactionStatus.VerificationUnavailable, result.Status);
        await publisher.DidNotReceive().PublishAsync(Arg.Any<PartnerTransactionMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_crash_on_unexpected_verification_errors()
    {
        var verifier = Substitute.For<IPartnerVerificationClient>();
        var publisher = Substitute.For<ITransactionQueuePublisher>();
        verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var processor = new PartnerTransactionProcessor(verifier, publisher, NullLogger<PartnerTransactionProcessor>.Instance);

        var result = await processor.ProcessAsync(ValidRequest(), CancellationToken.None);

        Assert.Equal(ProcessTransactionStatus.VerificationUnavailable, result.Status);
    }

    [Fact]
    public async Task Returns_publish_failed_when_queue_is_down()
    {
        var verifier = Substitute.For<IPartnerVerificationClient>();
        var publisher = Substitute.For<ITransactionQueuePublisher>();
        verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PartnerVerificationResponse("P-1001", true, "Acme"));
        publisher.PublishAsync(Arg.Any<PartnerTransactionMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("broker down"));

        var processor = new PartnerTransactionProcessor(verifier, publisher, NullLogger<PartnerTransactionProcessor>.Instance);

        var result = await processor.ProcessAsync(ValidRequest(), CancellationToken.None);

        Assert.Equal(ProcessTransactionStatus.PublishFailed, result.Status);
    }

    private static PartnerTransactionRequest ValidRequest() => new()
    {
        PartnerId = "P-1001",
        TransactionReference = "TXN-99823",
        Amount = 250.00m,
        Currency = "USD",
        Timestamp = DateTimeOffset.Parse("2024-05-10T14:30:00Z")
    };
}

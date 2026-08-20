using PartnerIntegration.Application.Contracts;

namespace PartnerIntegration.Application.Abstractions;

public interface ITransactionQueuePublisher
{
    Task PublishAsync(PartnerTransactionMessage message, CancellationToken cancellationToken);
}

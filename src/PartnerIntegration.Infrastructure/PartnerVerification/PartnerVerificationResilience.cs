using PartnerIntegration.Application.Contracts;
using Polly;
using Polly.Retry;

namespace PartnerIntegration.Infrastructure.PartnerVerification;

public static class PartnerVerificationResilience
{
    public static ResiliencePipeline<PartnerVerificationResponse> CreatePipeline(PartnerVerificationOptions options)
    {
        return new ResiliencePipelineBuilder<PartnerVerificationResponse>()
            .AddRetry(new RetryStrategyOptions<PartnerVerificationResponse>
            {
                ShouldHandle = new PredicateBuilder<PartnerVerificationResponse>()
                    .Handle<TimeoutException>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(Math.Max(0, options.InitialRetryDelayMilliseconds)),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false
            })
            .Build();
    }
}

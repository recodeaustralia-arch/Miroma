namespace PartnerIntegration.Application.Exceptions;

public sealed class PartnerVerificationUnavailableException : Exception
{
    public PartnerVerificationUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

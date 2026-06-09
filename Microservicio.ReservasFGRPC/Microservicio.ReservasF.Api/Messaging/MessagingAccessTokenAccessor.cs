namespace Microservicio.ReservasF.Api.Messaging;

public sealed class MessagingAccessTokenAccessor : IMessagingAccessTokenAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string? Token
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}

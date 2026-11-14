namespace ManualMate.Application.Contracts;

public record ItemCreatedMessage
{
    public const string RoutingKey = "item.created";

    public Guid ItemId { get; init; }
}

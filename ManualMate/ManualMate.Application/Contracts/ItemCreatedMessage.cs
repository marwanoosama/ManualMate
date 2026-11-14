namespace ManualMate.Application.Contracts;

public record ItemCreatedMessage
{
    public const string RoutingKey = "item.created";

    public int ItemId { get; init; }
}

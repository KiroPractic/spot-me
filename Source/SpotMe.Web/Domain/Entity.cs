namespace SpotMe.Web.Domain;

public class Entity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTimeOffset CreatedOn { get; private set; } = DateTimeOffset.UtcNow;
}
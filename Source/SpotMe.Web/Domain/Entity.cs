namespace SpotMe.Web.Domain;

public class Entity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CreatedOn { get; private set; } = DateTime.UtcNow;
}
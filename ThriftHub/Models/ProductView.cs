namespace ThriftHub.Models;

public class ProductView
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ViewerKey { get; set; } = string.Empty;

    public DateTime FirstViewedAt { get; set; } = DateTime.UtcNow;
}

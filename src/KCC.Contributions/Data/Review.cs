namespace KCC.Contributions.Data;

public class Review
{
    public int Id { get; set; }

    public Guid VariantKey { get; set; }

    public Guid MemberKey { get; set; }

    public decimal Rating { get; set; }

    public string Text { get; set; }

    public DateTime Created { get; set; }

    public DateTime Modified { get; set; }
}

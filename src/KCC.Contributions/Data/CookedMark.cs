namespace KCC.Contributions.Data;

public class CookedMark
{
    public int Id { get; set; }

    public Guid VariantKey { get; set; }

    public Guid MemberKey { get; set; }

    public DateTime Created { get; set; }
}

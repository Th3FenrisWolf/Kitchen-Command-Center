using KCC.Web.Features.Search;

namespace KCC.UnitTests.Features.Search;

public class RecipeSearchCriteriaTests
{
    [Test]
    [Arguments("relevant", RecipeSearchConstants.FieldName, false)]
    [Arguments("rated", RecipeSearchConstants.FieldAverageRating, true)]
    [Arguments("variants", RecipeSearchConstants.FieldVariantCount, true)]
    [Arguments("recent", RecipeSearchConstants.FieldPublished, true)]
    public async Task SortField_MapsKnownKeys(string sort, string field, bool descending)
    {
        var (name, desc, byScore) = RecipeSearchCriteria.SortSpec(sort);
        if (sort == "relevant")
        {
            _ = await Assert.That(byScore).IsTrue();
        }
        else
        {
            _ = await Assert.That(name).IsEqualTo(field);
            _ = await Assert.That(desc).IsEqualTo(descending);
        }
    }

    [Test]
    public async Task Clamp_NormalizesPagingAndTime()
    {
        var c = new RecipeSearchCriteria { Page = -3, PageSize = 999, TimeMin = 40, TimeMax = 10 }.Normalized();
        _ = await Assert.That(c.Page).IsEqualTo(0);
        _ = await Assert.That(c.PageSize).IsEqualTo(RecipeSearchCriteria.MaxPageSize);
        _ = await Assert.That(c.TimeMin).IsEqualTo(10);   // swapped
        _ = await Assert.That(c.TimeMax).IsEqualTo(40);
    }
}

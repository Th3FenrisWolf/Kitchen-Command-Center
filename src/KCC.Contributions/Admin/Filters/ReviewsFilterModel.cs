using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base.Filters;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace KCC.Contributions.Admin.Filters;

public class ReviewsFilterModel
{
    [DecimalNumberInputComponent(Label = "Max rating", Order = 0)]
    [FilterCondition(BuilderType = typeof(MaximumValueWhereBuilder), ColumnName = nameof(VariantReviewInfo.Rating))]
    public decimal? MaximumRating { get; set; }

    [DateInputComponent(Label = "Created from", Order = 1)]
    [FilterCondition(BuilderType = typeof(MinimumValueWhereBuilder), ColumnName = nameof(VariantReviewInfo.ReviewCreated))]
    public DateTime? CreatedFrom { get; set; }

    [DateInputComponent(Label = "Created to", Order = 2)]
    [FilterCondition(BuilderType = typeof(DateUpperBoundWhereBuilder), ColumnName = nameof(VariantReviewInfo.ReviewCreated))]
    public DateTime? CreatedTo { get; set; }
}

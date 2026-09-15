using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base.Filters;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace KCC.Contributions.Admin.Filters;

public class CookNotesFilterModel
{
    [DateInputComponent(Label = "Created from", Order = 0)]
    [FilterCondition(BuilderType = typeof(MinimumValueWhereBuilder), ColumnName = nameof(VariantCookNoteInfo.NoteCreated))]
    public DateTime? CreatedFrom { get; set; }

    [DateInputComponent(Label = "Created to", Order = 1)]
    [FilterCondition(BuilderType = typeof(DateUpperBoundWhereBuilder), ColumnName = nameof(VariantCookNoteInfo.NoteCreated))]
    public DateTime? CreatedTo { get; set; }
}

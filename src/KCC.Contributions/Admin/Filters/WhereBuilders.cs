using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.Filters;

namespace KCC.Contributions.Admin.Filters;

public class MaximumValueWhereBuilder : IWhereConditionBuilder
{
    public Task<IWhereCondition> Build(string columnName, object value) =>
        Task.FromResult<IWhereCondition>(value is null
            ? new WhereCondition()
            : new WhereCondition().WhereLessOrEquals(columnName, value));
}

public class MinimumValueWhereBuilder : IWhereConditionBuilder
{
    public Task<IWhereCondition> Build(string columnName, object value) =>
        Task.FromResult<IWhereCondition>(value is null
            ? new WhereCondition()
            : new WhereCondition().WhereGreaterOrEquals(columnName, value));
}

/// <summary>Brackets a date filter inclusively: rows on the selected day itself still match.</summary>
public class DateUpperBoundWhereBuilder : IWhereConditionBuilder
{
    public Task<IWhereCondition> Build(string columnName, object value) =>
        Task.FromResult<IWhereCondition>(value is DateTime date
            ? new WhereCondition().WhereLessThan(columnName, date.Date.AddDays(1))
            : new WhereCondition());
}

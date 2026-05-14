using CMS.DataEngine;

namespace KCC.ResourceStrings.Data;

/// <summary>
/// Declares members for <see cref="ResourceStringInfo"/> management.
/// </summary>
public partial interface IResourceStringInfoProvider : IInfoProvider<ResourceStringInfo>, IInfoByIdProvider<ResourceStringInfo>, IInfoByNameProvider<ResourceStringInfo>
{
}

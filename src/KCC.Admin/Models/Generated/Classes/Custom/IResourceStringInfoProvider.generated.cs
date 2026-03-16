using CMS.DataEngine;

namespace ResourceStrings
{
    /// <summary>
    /// Declares members for <see cref="ResourceStringInfo"/> management.
    /// </summary>
    public partial interface IResourceStringInfoProvider : IInfoProvider<ResourceStringInfo>, IInfoByIdProvider<ResourceStringInfo>, IInfoByNameProvider<ResourceStringInfo>
    {
    }
}
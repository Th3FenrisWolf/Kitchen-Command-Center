using CMS.DataEngine;

namespace ResourceStrings
{
    /// <summary>
    /// Class providing <see cref="ResourceStringInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IResourceStringInfoProvider))]
    public partial class ResourceStringInfoProvider : AbstractInfoProvider<ResourceStringInfo, ResourceStringInfoProvider>, IResourceStringInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceStringInfoProvider"/> class.
        /// </summary>
        public ResourceStringInfoProvider()
            : base(ResourceStringInfo.TYPEINFO)
        {
        }
    }
}
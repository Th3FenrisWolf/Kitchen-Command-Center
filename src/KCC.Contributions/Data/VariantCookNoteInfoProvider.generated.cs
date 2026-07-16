using CMS.DataEngine;

namespace KCC.Contributions.Data
{
    /// <summary>
    /// Class providing <see cref="VariantCookNoteInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IVariantCookNoteInfoProvider))]
    public partial class VariantCookNoteInfoProvider : AbstractInfoProvider<VariantCookNoteInfo, VariantCookNoteInfoProvider>, IVariantCookNoteInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VariantCookNoteInfoProvider"/> class.
        /// </summary>
        public VariantCookNoteInfoProvider() : base(VariantCookNoteInfo.TYPEINFO)
        {
        }
    }
}
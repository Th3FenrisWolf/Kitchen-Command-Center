using CMS.DataEngine;

namespace KCC.Contributions.Data
{
    /// <summary>
    /// Class providing <see cref="VariantCookedInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IVariantCookedInfoProvider))]
    public partial class VariantCookedInfoProvider : AbstractInfoProvider<VariantCookedInfo, VariantCookedInfoProvider>, IVariantCookedInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VariantCookedInfoProvider"/> class.
        /// </summary>
        public VariantCookedInfoProvider() : base(VariantCookedInfo.TYPEINFO)
        {
        }
    }
}
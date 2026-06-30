using CMS.DataEngine;

namespace KCC.Contributions.Data
{
    /// <summary>
    /// Class providing <see cref="VariantReviewInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IVariantReviewInfoProvider))]
    public partial class VariantReviewInfoProvider : AbstractInfoProvider<VariantReviewInfo, VariantReviewInfoProvider>, IVariantReviewInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VariantReviewInfoProvider"/> class.
        /// </summary>
        public VariantReviewInfoProvider()
            : base(VariantReviewInfo.TYPEINFO)
        {
        }
    }
}
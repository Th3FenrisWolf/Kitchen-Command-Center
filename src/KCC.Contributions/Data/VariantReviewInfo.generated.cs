using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using KCC.Contributions.Data;

[assembly: RegisterObjectType(typeof(VariantReviewInfo), VariantReviewInfo.OBJECT_TYPE)]

namespace KCC.Contributions.Data
{
    /// <summary>
    /// Data container class for <see cref="VariantReviewInfo"/>.
    /// </summary>
    public partial class VariantReviewInfo : AbstractInfo<VariantReviewInfo, IVariantReviewInfoProvider>, IInfoWithId
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kcc.variantreview";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(VariantReviewInfoProvider), OBJECT_TYPE, "KCC.VariantReview", "VariantReviewID", null, null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
        };


        /// <summary>
        /// Variant review ID.
        /// </summary>
        [DatabaseField]
        public virtual int VariantReviewID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(VariantReviewID)), 0);
            set => SetValue(nameof(VariantReviewID), value);
        }


        /// <summary>
        /// Variant guid.
        /// </summary>
        [DatabaseField]
        public virtual Guid VariantGuid
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(VariantGuid)), Guid.Empty);
            set => SetValue(nameof(VariantGuid), value);
        }


        /// <summary>
        /// Recipe guid.
        /// </summary>
        [DatabaseField]
        public virtual Guid RecipeGuid
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(RecipeGuid)), Guid.Empty);
            set => SetValue(nameof(RecipeGuid), value);
        }


        /// <summary>
        /// Member guid.
        /// </summary>
        [DatabaseField]
        public virtual Guid MemberGuid
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(MemberGuid)), Guid.Empty);
            set => SetValue(nameof(MemberGuid), value);
        }


        /// <summary>
        /// Rating.
        /// </summary>
        [DatabaseField]
        public virtual int Rating
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(Rating)), 0);
            set => SetValue(nameof(Rating), value);
        }


        /// <summary>
        /// Review text.
        /// </summary>
        [DatabaseField]
        public virtual string ReviewText
        {
            get => ValidationHelper.GetString(GetValue(nameof(ReviewText)), String.Empty);
            set => SetValue(nameof(ReviewText), value, String.Empty);
        }


        /// <summary>
        /// Review created.
        /// </summary>
        [DatabaseField]
        public virtual DateTime ReviewCreated
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(ReviewCreated)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(ReviewCreated), value);
        }


        /// <summary>
        /// Review modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime ReviewModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(ReviewModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(ReviewModified), value);
        }


        /// <summary>
        /// Deletes the object using appropriate provider.
        /// </summary>
        protected override void DeleteObject()
        {
            Provider.Delete(this);
        }


        /// <summary>
        /// Updates the object using appropriate provider.
        /// </summary>
        protected override void SetObject()
        {
            Provider.Set(this);
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="VariantReviewInfo"/> class.
        /// </summary>
        public VariantReviewInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="VariantReviewInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public VariantReviewInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
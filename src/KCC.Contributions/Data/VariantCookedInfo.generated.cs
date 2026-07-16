using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using KCC.Contributions.Data;

[assembly: RegisterObjectType(typeof(VariantCookedInfo), VariantCookedInfo.OBJECT_TYPE)]

namespace KCC.Contributions.Data
{
    /// <summary>
    /// Data container class for <see cref="VariantCookedInfo"/>.
    /// </summary>
    public partial class VariantCookedInfo : AbstractInfo<VariantCookedInfo, IVariantCookedInfoProvider>, IInfoWithId
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kcc.variantcooked";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(VariantCookedInfoProvider), OBJECT_TYPE, "KCC.VariantCooked", "VariantCookedID", null, null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
        };


        /// <summary>
        /// Variant cooked ID.
        /// </summary>
        [DatabaseField]
        public virtual int VariantCookedID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(VariantCookedID)), 0);
            set => SetValue(nameof(VariantCookedID), value);
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
        /// Cooked created.
        /// </summary>
        [DatabaseField]
        public virtual DateTime CookedCreated
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(CookedCreated)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(CookedCreated), value);
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
        /// Creates an empty instance of the <see cref="VariantCookedInfo"/> class.
        /// </summary>
        public VariantCookedInfo() : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="VariantCookedInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public VariantCookedInfo(DataRow dr) : base(TYPEINFO, dr)
        {
        }
    }
}
using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using KCC.Contributions.Data;

[assembly: RegisterObjectType(typeof(VariantCookNoteInfo), VariantCookNoteInfo.OBJECT_TYPE)]

namespace KCC.Contributions.Data
{
    /// <summary>
    /// Data container class for <see cref="VariantCookNoteInfo"/>.
    /// </summary>
    public partial class VariantCookNoteInfo : AbstractInfo<VariantCookNoteInfo, IVariantCookNoteInfoProvider>, IInfoWithId
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kcc.variantcooknote";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(VariantCookNoteInfoProvider), OBJECT_TYPE, "KCC.VariantCookNote", "VariantCookNoteID", null, null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
        };


        /// <summary>
        /// Variant cook note ID.
        /// </summary>
        [DatabaseField]
        public virtual int VariantCookNoteID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(VariantCookNoteID)), 0);
            set => SetValue(nameof(VariantCookNoteID), value);
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
        /// Note text.
        /// </summary>
        [DatabaseField]
        public virtual string NoteText
        {
            get => ValidationHelper.GetString(GetValue(nameof(NoteText)), String.Empty);
            set => SetValue(nameof(NoteText), value);
        }


        /// <summary>
        /// Note created.
        /// </summary>
        [DatabaseField]
        public virtual DateTime NoteCreated
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(NoteCreated)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(NoteCreated), value);
        }


        /// <summary>
        /// Note modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime NoteModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(NoteModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(NoteModified), value);
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
        /// Creates an empty instance of the <see cref="VariantCookNoteInfo"/> class.
        /// </summary>
        public VariantCookNoteInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="VariantCookNoteInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public VariantCookNoteInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
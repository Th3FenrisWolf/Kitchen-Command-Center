using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using ResourceStrings;

[assembly: RegisterObjectType(typeof(ResourceStringTranslationInfo), ResourceStringTranslationInfo.OBJECT_TYPE)]

namespace ResourceStrings
{
    public partial class ResourceStringTranslationInfo : AbstractInfo<ResourceStringTranslationInfo, IInfoProvider<ResourceStringTranslationInfo>>, IInfoWithId
    {
        public const string OBJECT_TYPE = "custom.resourcestringtranslation";

        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<ResourceStringTranslationInfo>), OBJECT_TYPE, "Custom.ResourceStringTranslation", "ResourceStringTranslationID", null, null, null, null, null, "ResourceStringTranslationResourceStringID", ResourceStringInfo.OBJECT_TYPE)
        {
            TouchCacheDependencies = true,
        };

        [DatabaseField]
        public virtual int ResourceStringTranslationID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(ResourceStringTranslationID)), 0);
            set => SetValue(nameof(ResourceStringTranslationID), value);
        }

        [DatabaseField]
        public virtual int ResourceStringTranslationResourceStringID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(ResourceStringTranslationResourceStringID)), 0);
            set => SetValue(nameof(ResourceStringTranslationResourceStringID), value);
        }

        [DatabaseField]
        public virtual int ResourceStringTranslationContentLanguageID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(ResourceStringTranslationContentLanguageID)), 0);
            set => SetValue(nameof(ResourceStringTranslationContentLanguageID), value);
        }

        [DatabaseField]
        public virtual string ResourceStringTranslationValue
        {
            get => ValidationHelper.GetString(GetValue(nameof(ResourceStringTranslationValue)), String.Empty);
            set => SetValue(nameof(ResourceStringTranslationValue), value);
        }

        protected override void DeleteObject()
        {
            Provider.Delete(this);
        }

        protected override void SetObject()
        {
            Provider.Set(this);
        }

        public ResourceStringTranslationInfo()
            : base(TYPEINFO)
        {
        }

        public ResourceStringTranslationInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}

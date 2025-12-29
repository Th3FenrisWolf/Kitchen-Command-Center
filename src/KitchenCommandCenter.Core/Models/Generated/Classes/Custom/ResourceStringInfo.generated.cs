using System;
using System.Data;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using ResourceStrings;

[assembly: RegisterObjectType(typeof(ResourceStringInfo), ResourceStringInfo.OBJECT_TYPE)]

namespace ResourceStrings
{
    /// <summary>
    /// Data container class for <see cref="ResourceStringInfo"/>.
    /// </summary>
    public partial class ResourceStringInfo
        : AbstractInfo<ResourceStringInfo, IResourceStringInfoProvider>,
            IInfoWithId,
            IInfoWithName
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "custom.resourcestring";

        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(
            typeof(ResourceStringInfoProvider),
            OBJECT_TYPE,
            "Custom.ResourceString",
            "ResourceStringID",
            null,
            null,
            "Key",
            "Value",
            null,
            null,
            null
        )
        {
            TouchCacheDependencies = true,
        };

        /// <summary>
        /// Resource string ID.
        /// </summary>
        [DatabaseField]
        public virtual int ResourceStringID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(ResourceStringID)), 0);
            set => SetValue(nameof(ResourceStringID), value);
        }

        /// <summary>
        /// Key.
        /// </summary>
        [DatabaseField]
        public virtual string Key
        {
            get => ValidationHelper.GetString(GetValue(nameof(Key)), String.Empty);
            set => SetValue(nameof(Key), value);
        }

        /// <summary>
        /// Value.
        /// </summary>
        [DatabaseField]
        public virtual string Value
        {
            get => ValidationHelper.GetString(GetValue(nameof(Value)), String.Empty);
            set => SetValue(nameof(Value), value);
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
        /// Creates an empty instance of the <see cref="ResourceStringInfo"/> class.
        /// </summary>
        public ResourceStringInfo()
            : base(TYPEINFO) { }

        /// <summary>
        /// Creates a new instances of the <see cref="ResourceStringInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public ResourceStringInfo(DataRow dr)
            : base(TYPEINFO, dr) { }
    }
}


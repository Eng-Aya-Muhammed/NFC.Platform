using System;

namespace NFC.Platform.BuildingBlocks.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ExportColumnAttribute : Attribute
    {
        public string ResourceKey { get; }
        public int Order { get; set; }

        public ExportColumnAttribute(string resourceKey, int order = 0)
        {
            ResourceKey = resourceKey;
            Order = order;
        }
    }
}

using System;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class RolePermission : BaseEntity
    {
        public Guid RoleId { get; set; }

        public string Permission { get; set; } = string.Empty;

        public Role Role { get; set; } = null!;
    }
}

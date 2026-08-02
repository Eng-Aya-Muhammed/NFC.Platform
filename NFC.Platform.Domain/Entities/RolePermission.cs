using System;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public class RolePermission : BaseEntity
#pragma warning restore CA1711
    {
        public Guid RoleId { get; set; }

        public string Permission { get; set; } = string.Empty;

        public Role Role { get; set; } = null!;
    }
}

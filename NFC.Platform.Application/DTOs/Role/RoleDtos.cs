using System;
using System.Collections.Generic;

namespace NFC.Platform.Application.DTOs.Role
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; }
        public IReadOnlyList<string> Permissions { get; set; } = [];
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public IReadOnlyList<string> Permissions { get; set; } = [];
    }

    public class AssignPermissionsRequest
    {
        public IReadOnlyList<string> Permissions { get; set; } = [];
    }

    public class AssignRoleToUserRequest
    {
        public Guid RoleId { get; set; }
    }
}

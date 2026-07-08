using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class Company : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string CommercialRegistry { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public Guid AdminUserId { get; set; }
        public User AdminUser { get; set; } = null!;

        public ICollection<User> Employees { get; set; } = new List<User>();
        public ICollection<CardOrder> CardOrders { get; set; } = new List<CardOrder>();
    }
}

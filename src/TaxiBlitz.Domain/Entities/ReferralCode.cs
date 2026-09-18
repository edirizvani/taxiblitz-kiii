using System;
using System.Collections.Generic;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Domain.Entities
{
    public class ReferralCode
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string OwnerId { get; set; }
        public decimal DiscountPercent { get; set; } = 5;
        public int UsageCount { get; set; }
        public int MaxUses { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser Owner { get; set; }
        public ICollection<ReferralUsage> Usages { get; set; }
    }
}

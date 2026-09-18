using System;
using System.Collections.Generic;

namespace TaxiBlitz.Application.ViewModels
{
    public class ReceptionistCodeRowViewModel
    {
        public int      CodeId           { get; set; }
        public string   Code             { get; set; } = string.Empty;
        public bool     IsActive         { get; set; }
        public DateTime CreatedAt        { get; set; }
        public string?  UsedByEmail      { get; set; }
        public DateTime? BookingDate     { get; set; }
        public string   BookingStatus    { get; set; } = "Not Used";
        public decimal? CommissionAmount { get; set; }
        public bool     CommissionPaid   { get; set; }
        public DateTime? CommissionPaidAt { get; set; }
    }

    public class ReceptionistDashboardViewModel
    {
        public decimal TotalEarned  { get; set; }
        public decimal TotalPaid    { get; set; }
        public decimal TotalPending { get; set; }
        public IList<ReceptionistCodeRowViewModel> Codes { get; set; } = new List<ReceptionistCodeRowViewModel>();
    }

    public class ReceptionistSummaryViewModel
    {
        public string  UserId              { get; set; } = string.Empty;
        public string  FullName            { get; set; } = string.Empty;
        public string  Email               { get; set; } = string.Empty;
        public int     TotalCodesGenerated { get; set; }
        public int     TotalBookings       { get; set; }
        public int     ApprovedBookings    { get; set; }
        public decimal TotalEarned         { get; set; }
        public decimal PendingCommission   { get; set; }
    }

    public class AdminReceptionistDetailViewModel
    {
        public string  ReceptionistEmail { get; set; } = string.Empty;
        public string  ReceptionistName  { get; set; } = string.Empty;
        public ReceptionistDashboardViewModel Dashboard { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class BackupLog
    {
        public int BackupId { get; set; }

        public int TenantId { get; set; }
        public int PerformedByUserId { get; set; }
        public DateTime BackupDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? FileLocation { get; set; }
    }
}
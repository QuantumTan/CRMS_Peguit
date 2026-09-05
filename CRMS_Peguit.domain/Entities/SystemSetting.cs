using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class SystemSetting
    {
        public int SettingId { get; set; }

        public int TenantId { get; set; }
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public int UpdatedByUserId { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
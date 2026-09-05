using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.Entities
{
    public class Role
    {
        public int RoleId { get; set; }

        public int TenantId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}


using NEXA.Model;
namespace RealEstateCRM.Entities;

public class LoginSession
{
    public int SessionID { get; set; }
    public int UserID { get; set; }
    public DateTime LoginAt { get; set; }
    public DateTime? LogoutAt { get; set; }
    public string? IPAddress { get; set; }

    public User? User { get; set; }
}
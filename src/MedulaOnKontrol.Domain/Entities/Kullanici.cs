namespace MedulaOnKontrol.Domain.Entities;
public sealed class Kullanici : Varlik
{
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public long KurumId { get; set; }
    public bool IsActive { get; set; }
    public string[] Roller { get; set; } = [];
    public string[] Klinikler { get; set; } = [];
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
}

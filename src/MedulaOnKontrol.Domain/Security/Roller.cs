namespace MedulaOnKontrol.Domain.Security;
public static class Roller
{
    public const string BillingOfficer = "Fatura Görevlisi", CodingSpecialist = "Kodlama Uzmanı", RevenueOfficer = "Gelir Tahakkuk Sorumlusu", RuleAdministrator = "Kural Yöneticisi", Manager = "Yönetici", InternalAuditor = "İç Denetçi", SystemAdministrator = "Sistem Yöneticisi";
    public static readonly string[] All = [BillingOfficer, CodingSpecialist, RevenueOfficer, RuleAdministrator, Manager, InternalAuditor, SystemAdministrator];
}

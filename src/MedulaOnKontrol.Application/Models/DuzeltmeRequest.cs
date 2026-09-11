using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed class DuzeltmeRequest
{
    public long FaturaId { get; set; }
    public long Revision { get; set; }
    public long? KalemId { get; set; }
    public string Kind { get; set; } = "line";
    public string? Code { get; set; }
    public decimal Adet { get; set; }
    public decimal BirimFiyat { get; set; }
    public DateTime KayitTarihi { get; set; }
    public bool PaketMi { get; set; }
    public bool IsConditionSatisfied { get; set; }
    public BelgeTip BelgeTip { get; set; }
    public ImzaDurum Signature { get; set; }
    public DateTime? GecerlilikBitis { get; set; }
    public string Gerekce { get; set; } = "";
}

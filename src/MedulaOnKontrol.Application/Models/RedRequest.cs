using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed class RedRequest
{
    public long FaturaId { get; set; }
    public long? KalemId { get; set; }
    public string SgkRedKodu { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Tutar { get; set; }
    public DateTime KayitTarihi { get; set; }
    public string? KuralKodu { get; set; }
}

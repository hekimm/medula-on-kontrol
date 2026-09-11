using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed class FaturaFilter
{
    public int? Donem { get; set; }
    public string? Klinik { get; set; }
    public BasvuruTip? BasvuruTip { get; set; }
    public FaturaDurum? Status { get; set; }
    public decimal RiskMin { get; set; }
    public decimal RiskMax { get; set; } = 100;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string SortBy { get; set; } = "risk";
    public string SortDirection { get; set; } = "descending";
}

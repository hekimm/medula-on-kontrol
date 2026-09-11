using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed class GostergePaneli
{
    public int FaturaSayisi { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal RisktekiTutar { get; set; }
    public int EngelleyiciFatura { get; set; }
    public decimal OnlenenTahminiTutar { get; set; }
    public List<OzetSatiri> Klinikler { get; set; } = [];
    public List<OzetSatiri> Bulgular { get; set; } = [];
    public List<DonemTrend> Trend { get; set; } = [];
}

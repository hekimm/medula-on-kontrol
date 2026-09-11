using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed class KuralMetrik
{
    public string KuralKodu { get; set; } = "";
    public int TruePositiveCount { get; set; }
    public int FalsePositiveCount { get; set; }
    public int FalseNegativeCount { get; set; }
    public decimal? Precision => TruePositiveCount + FalsePositiveCount == 0 ? null : (decimal)TruePositiveCount / (TruePositiveCount + FalsePositiveCount);
    public decimal? Recall => TruePositiveCount + FalseNegativeCount == 0 ? null : (decimal)TruePositiveCount / (TruePositiveCount + FalseNegativeCount);
    public decimal? F1 => 2 * TruePositiveCount + FalsePositiveCount + FalseNegativeCount == 0 ? null : (decimal)(2 * TruePositiveCount) / (2 * TruePositiveCount + FalsePositiveCount + FalseNegativeCount);
}

using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed class KuralVersionRequest
{
    public string KuralKodu { get; set; } = "";
    public int ExpectedVersion { get; set; }
    public Severity Severity { get; set; }
    public int Agirlik { get; set; }
    public string ParametersJson { get; set; } = "{}";
    public DateTime StartedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string Gerekce { get; set; } = "";
}

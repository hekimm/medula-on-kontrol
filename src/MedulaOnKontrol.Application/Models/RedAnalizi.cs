using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed record RedAnalizi(IReadOnlyList<RedKaydi> Redler, IReadOnlyList<KuralMetrik> Metrics, IReadOnlyList<OzetSatiri> Suggestions);

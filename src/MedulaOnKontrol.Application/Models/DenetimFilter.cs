using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed class DenetimFilter
{
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public long? UserId { get; set; }
    public DenetimIslem? Operation { get; set; }
    public string? Varlik { get; set; }
    public int PageNumber { get; set; } = 1;
}

using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed class IstisnaRequest
{
    public long FaturaId { get; set; }
    public long BulguId { get; set; }
    public string Gerekce { get; set; } = "";
    public DateTime GecerlilikBitis { get; set; }
}

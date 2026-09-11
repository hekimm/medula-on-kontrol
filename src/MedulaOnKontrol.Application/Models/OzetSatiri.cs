using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed record OzetSatiri(string Name, decimal Tutar, int Adet)
{
    public OzetSatiri() : this("", 0, 0)
    {
    }
}

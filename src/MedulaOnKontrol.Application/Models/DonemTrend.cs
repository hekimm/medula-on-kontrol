using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed record DonemTrend(int Donem, int SubmittedCount, int RejectedCount)
{
    public DonemTrend() : this(0, 0, 0)
    {
    }
}

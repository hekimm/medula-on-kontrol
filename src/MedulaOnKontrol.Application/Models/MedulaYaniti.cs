using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed record MedulaYaniti(bool IsAccepted, string Reference, string? RedKodu);

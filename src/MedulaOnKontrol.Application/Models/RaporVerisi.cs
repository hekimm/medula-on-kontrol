using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed record RaporVerisi(GostergePaneli Summary, IReadOnlyList<Fatura> Faturalar, IReadOnlyList<Bulgu> Bulgular);

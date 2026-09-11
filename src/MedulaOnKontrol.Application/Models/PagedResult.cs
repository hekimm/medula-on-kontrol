using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Models;
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize);

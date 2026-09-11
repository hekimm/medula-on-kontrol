using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface IKullaniciRepository
{
    Task<Kullanici?> FindAsync(string username, CancellationToken cancellationToken);
    Task<Kullanici?> GetAsync(long id, CancellationToken cancellationToken);
    Task RecordLoginResultAsync(long id, bool isSuccessful, CancellationToken cancellationToken);
}

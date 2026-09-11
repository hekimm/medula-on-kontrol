using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Contracts;
public interface IFaturaRepository
{
    Task<PagedResult<Fatura>> ListAsync(ErisimKapsami scope, FaturaFilter filter, CancellationToken cancellationToken);
    Task<FaturaDenetimBaglami?> GetAsync(ErisimKapsami scope, long id, CancellationToken cancellationToken);
    Task<IReadOnlyList<FaturaDenetimBaglami>> LoadPeriodAsync(ErisimKapsami scope, int donem, CancellationToken cancellationToken);
    Task<IReadOnlyList<Bulgu>> ListBulgularAsync(ErisimKapsami scope, long id, CancellationToken cancellationToken);
    Task<Result<KontrolSonucu>> SaveValidationAsync(ErisimKapsami scope, FaturaDenetimBaglami context, KontrolSonucu result, CancellationToken cancellationToken);
    Task<Result<bool>> CorrectAsync(ErisimKapsami scope, DuzeltmeRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> AddExceptionAsync(ErisimKapsami scope, IstisnaRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> ApproveAsync(ErisimKapsami scope, long id, CancellationToken cancellationToken);
    Task<GostergePaneli> GetSummaryAsync(ErisimKapsami scope, int donem, CancellationToken cancellationToken);
    Task<RaporVerisi> GetReportAsync(ErisimKapsami scope, int donem, CancellationToken cancellationToken);
    Task<IReadOnlyList<OzetSatiri>> ListKliniklerAsync(ErisimKapsami scope, CancellationToken cancellationToken);
}

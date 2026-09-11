using System.Diagnostics;
using MedulaOnKontrol.Domain;

namespace MedulaOnKontrol.Application.Services;
public sealed class DonemDenetimService(IFaturaRepository faturalar, IKuralRepository kurallar, IDenetimIsiRepository jobs, KuralEngine engine)
{
    public async Task<DonemDenetimSonucu> RunAsync(DenetimIsi operation, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var candidates = (await jobs.GetCandidatesAsync(operation, cancellationToken)).ToHashSet();
        var candidateSelectionSeconds = stopwatch.Elapsed.TotalSeconds;
        var scope = new ErisimKapsami(operation.UserId, operation.KurumId, "donem-denetimi");
        var contexts = (await faturalar.LoadPeriodAsync(scope, operation.Donem, cancellationToken)).Where(context => candidates.Contains(context.Fatura.Id)).ToList();
        var catalog = await kurallar.ListAsync(cancellationToken);
        var completedCount = 0;
        var failedCount = 0;
        var bulguSayisi = 0;
        var contextLoadSeconds = stopwatch.Elapsed.TotalSeconds - candidateSelectionSeconds;
        await jobs.ProgressAsync(operation.Id, contexts.Count, 0, 0, null, false, cancellationToken);
        using var progressLock = new SemaphoreSlim(1, 1);
        await Parallel.ForEachAsync(contexts, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellationToken }, async (context, token) =>
        {
            var evaluation = (await engine.EvaluateAsync(context, catalog, token));
            var result = evaluation.IsSuccess ? await faturalar.SaveValidationAsync(scope, context, evaluation.Value!, token) : evaluation;
            if (!result.IsSuccess)
                Interlocked.Increment(ref failedCount);
            else
                Interlocked.Add(ref bulguSayisi, result.Value!.Bulgular.Count);
            if (Interlocked.Increment(ref completedCount) % 100 == 0)
            {
                await progressLock.WaitAsync(token);
                try
                {
                    await jobs.ProgressAsync(operation.Id, contexts.Count, Volatile.Read(ref completedCount), Volatile.Read(ref failedCount), null, false, token);
                }
                finally
                {
                    progressLock.Release();
                }
            }
        });
        await jobs.ProgressAsync(operation.Id, contexts.Count, completedCount, failedCount, failedCount > 0 ? "Bazı faturalar değişti; yeniden kontrol gerekir." : null, true, cancellationToken);
        return new(contexts.Count, contexts.Sum(context => context.Kalemler.Count), bulguSayisi, failedCount)
        {
            StageDurationsSeconds = new Dictionary<string, double>
            {
                {
                    "CandidateSelection",
                    candidateSelectionSeconds
                },
                {
                    "ContextLoading",
                    contextLoadSeconds
                },
                {
                    "EvaluationAndPersistence",
                    stopwatch.Elapsed.TotalSeconds - candidateSelectionSeconds - contextLoadSeconds
                }
            }
        };
    }
}

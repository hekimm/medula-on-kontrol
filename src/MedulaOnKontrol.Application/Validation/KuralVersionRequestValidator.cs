using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Validation;
public sealed class KuralVersionRequestValidator : AbstractValidator<KuralVersionRequest>
{
    public KuralVersionRequestValidator()
    {
        RuleFor(kuralVersionRequest => kuralVersionRequest.KuralKodu).Matches("^(PRV|TANI|ITU|ISL|TUT|BLG|ILC|SUR)-[0-9]{3}$");
        RuleFor(kuralVersionRequest => kuralVersionRequest.Agirlik).InclusiveBetween(1, 100);
        RuleFor(kuralVersionRequest => kuralVersionRequest.Severity).IsInEnum();
        RuleFor(kuralVersionRequest => kuralVersionRequest.Gerekce).MinimumLength(10).MaximumLength(1000);
        RuleFor(kuralVersionRequest => kuralVersionRequest.StartedAt).Must(date => date.Day == 1 && date.Year >= 2020 && date.Year <= 2100).WithMessage("Yürürlük tarihi ayın ilk günü olmalıdır.");
        RuleFor(kuralVersionRequest => kuralVersionRequest.ParametersJson).MaximumLength(2000).Must(IsValidJson).WithMessage("Yalnızca tolerans (0–1), periyotGun (1–3650), azamiAdet (1–10000) ve ekGun (0–365) sayısal parametreleri kabul edilir.");
    }

    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 2000)
            return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            var names = new HashSet<string>();
            foreach (var jsonProperty in doc.RootElement.EnumerateObject())
            {
                if (!names.Add(jsonProperty.Name) || jsonProperty.Value.ValueKind != JsonValueKind.Number || !jsonProperty.Value.TryGetDecimal(out var amount))
                    return false;
                if (!(jsonProperty.Name switch
                {
                    "tolerans" => amount is >= 0 and <= 1,
                    "periyotGun" => amount is >= 1 and <= 3650 && amount == decimal.Truncate(amount),
                    "azamiAdet" => amount is >= 1 and <= 10000 && amount == decimal.Truncate(amount),
                    "ekGun" => amount is >= 0 and <= 365 && amount == decimal.Truncate(amount),
                    _ => false
                }))
                    return false;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

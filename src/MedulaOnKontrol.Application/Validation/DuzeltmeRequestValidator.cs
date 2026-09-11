using System.Text.Json;
using FluentValidation;
using MedulaOnKontrol.Domain;
using Severity = MedulaOnKontrol.Domain.Enums.Severity;

namespace MedulaOnKontrol.Application.Validation;
public sealed class DuzeltmeRequestValidator : AbstractValidator<DuzeltmeRequest>
{
    public DuzeltmeRequestValidator()
    {
        RuleFor(duzeltmeRequest => duzeltmeRequest.FaturaId).GreaterThan(0);
        RuleFor(duzeltmeRequest => duzeltmeRequest.Gerekce).MinimumLength(10).MaximumLength(1000);
        RuleFor(duzeltmeRequest => duzeltmeRequest.Kind).Must(textValue => new[] { "line", "tani", "doc", "authorization", "total" }.Contains(textValue));
        When(duzeltmeRequest => duzeltmeRequest.Kind == "line", () =>
        {
            RuleFor(duzeltmeRequest => duzeltmeRequest.KalemId).NotNull();
            RuleFor(duzeltmeRequest => duzeltmeRequest.Adet).GreaterThan(0).LessThanOrEqualTo(10000).Must(amount => amount == decimal.Truncate(amount));
            RuleFor(duzeltmeRequest => duzeltmeRequest.BirimFiyat).InclusiveBetween(0, 10000000);
            RuleFor(duzeltmeRequest => duzeltmeRequest.Code).NotEmpty().MaximumLength(30);
        });
        When(duzeltmeRequest => duzeltmeRequest.Kind is "tani" or "authorization", () => RuleFor(duzeltmeRequest => duzeltmeRequest.Code).NotEmpty().MaximumLength(60));
        When(duzeltmeRequest => duzeltmeRequest.Kind is "line" or "doc", () => RuleFor(duzeltmeRequest => duzeltmeRequest.KayitTarihi).GreaterThan(new DateTime(2000, 1, 1)).LessThanOrEqualTo(DateTime.Today));
        RuleFor(duzeltmeRequest => duzeltmeRequest.BelgeTip).IsInEnum();
        RuleFor(duzeltmeRequest => duzeltmeRequest.Signature).IsInEnum();
        When(duzeltmeRequest => duzeltmeRequest.GecerlilikBitis.HasValue, () => RuleFor(duzeltmeRequest => duzeltmeRequest.GecerlilikBitis).GreaterThanOrEqualTo(duzeltmeRequest => duzeltmeRequest.KayitTarihi));
    }
}

using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MedulaOnKontrol.Web.ModelBinding;
public sealed class DecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None)
            return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        var text = value.FirstValue;
        var culture = text?.Contains(',') == true ? CultureInfo.GetCultureInfo("tr-TR") : CultureInfo.InvariantCulture;
        if (decimal.TryParse(text, NumberStyles.Number, culture, out var result))
            context.Result = ModelBindingResult.Success(result);
        else
            context.ModelState.TryAddModelError(context.ModelName, "Geçerli bir sayı girin.");
        return Task.CompletedTask;
    }
}

using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MedulaOnKontrol.Web.ModelBinding;
public sealed class DecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) => context.Metadata.ModelType == typeof(decimal) ? new DecimalModelBinder() : null;
}

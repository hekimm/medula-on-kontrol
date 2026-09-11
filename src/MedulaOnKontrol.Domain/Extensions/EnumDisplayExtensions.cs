using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MedulaOnKontrol.Domain.Extensions;

public static class EnumDisplayExtensions
{
    public static string ToDisplayName(this Enum value) =>
        value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName()
        ?? value.ToString();
}

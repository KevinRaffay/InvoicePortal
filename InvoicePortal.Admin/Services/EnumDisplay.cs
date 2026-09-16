using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace InvoicePortal.Admin.Services;

public static class EnumDisplay
{
    /// <summary>Returns the [Display(Name)] of an enum member, falling back to the member name.</summary>
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
    }

    /// <summary>Dropdown-friendly list of (Value, Text) pairs for an enum type.</summary>
    public static IReadOnlyList<EnumOption<TEnum>> Options<TEnum>() where TEnum : struct, Enum
        => Enum.GetValues<TEnum>().Select(v => new EnumOption<TEnum>(v, v.GetDisplayName())).ToList();

    public static string DisplayNameOrEmpty<TEnum>(int? value) where TEnum : struct, Enum
        => value.HasValue && Enum.IsDefined(typeof(TEnum), value.Value)
            ? ((TEnum)(object)value.Value).GetDisplayName()
            : value?.ToString() ?? string.Empty;
}

public sealed record EnumOption<TEnum>(TEnum Value, string Text) where TEnum : struct, Enum;

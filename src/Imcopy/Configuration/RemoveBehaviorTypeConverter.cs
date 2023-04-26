using System.ComponentModel;
using System.Globalization;

namespace Imcopy.Configuration;

internal class RemoveBehaviorTypeConverter: TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return value is string stringValue 
            ? stringValue switch
            {
                "keep" => RemoveBehavior.Keep,
                "remove" => RemoveBehavior.Remove,
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Invalid overwrite behavior."),
            }
            : base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        return value is RemoveBehavior removeBehavior && destinationType == typeof(string)
            ? removeBehavior switch
            {
                RemoveBehavior.Keep => "keep",
                RemoveBehavior.Remove => "remove",
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Invalid overwrite behavior."),
            }
            : base.ConvertTo(context, culture, value, destinationType);
    }
}

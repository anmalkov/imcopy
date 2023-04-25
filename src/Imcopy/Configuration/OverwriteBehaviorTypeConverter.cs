using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imcopy.Configuration;

internal class OverwriteBehaviorTypeConverter: TypeConverter
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
                "always" => OverwriteBehavior.Always,
                "ifNewer" => OverwriteBehavior.IfNewer,
                "never" => OverwriteBehavior.Never,
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
        return value is OverwriteBehavior overwriteBehavior && destinationType == typeof(string)
            ? overwriteBehavior switch
            {
                OverwriteBehavior.Always => "always",
                OverwriteBehavior.IfNewer => "ifNewer",
                OverwriteBehavior.Never => "never",
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Invalid overwrite behavior."),
            }
            : base.ConvertTo(context, culture, value, destinationType);
    }
}

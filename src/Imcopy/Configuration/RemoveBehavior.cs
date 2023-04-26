using System.ComponentModel;

namespace Imcopy.Configuration;

[TypeConverter(typeof(RemoveBehaviorTypeConverter))]
public enum RemoveBehavior
{
    Keep,
    Remove
};

using System.ComponentModel;

namespace Imcopy.Configuration;

[TypeConverter(typeof(OverwriteBehaviorTypeConverter))]
public enum OverwriteBehavior
{
    Always,
    IfNewer,
    Never
};

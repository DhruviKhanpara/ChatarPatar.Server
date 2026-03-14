using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ChatarPatar.Common.AppLogging.Extensions;

public static class EntityEntryExtensions
{
    public static long GetLongFromProperty(this EntityEntry change, string propName, long defaultValue = 0)
    {
        long? value = GetNullableLongFromProperty(change, propName);
        return value ?? defaultValue;
    }

    public static long? GetNullableLongFromProperty(this EntityEntry change, string propName)
    {
        long? returnValue = null;

        bool hasCurrentValue = change.CurrentValues.TryGetValue<long>(propName, out long currentValue);
        bool hasOriginalValue = change.OriginalValues.TryGetValue<long>(propName, out long originalValue);

        if (hasCurrentValue) returnValue = currentValue;
        else if (hasOriginalValue) returnValue = originalValue;

        return returnValue;
    }

    public static Guid GetGuidFromProperty(this EntityEntry change, string propName, Guid defaultValue = default)
    {
        Guid? value = GetNullableGuidFromProperty(change, propName);
        return value ?? defaultValue;
    }

    public static Guid? GetNullableGuidFromProperty(this EntityEntry change, string propName)
    {
        Guid? returnValue = null;

        bool hasCurrentValue = change.CurrentValues.TryGetValue<Guid?>(propName, out Guid? currentValue);
        bool hasOriginalValue = change.OriginalValues.TryGetValue<Guid?>(propName, out Guid? originalValue);

        if (hasCurrentValue) returnValue = currentValue;
        else if (hasOriginalValue) returnValue = originalValue;

        return returnValue;
    }
}

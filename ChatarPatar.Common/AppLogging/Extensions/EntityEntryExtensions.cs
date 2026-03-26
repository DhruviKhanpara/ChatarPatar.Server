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
        if (change.CurrentValues.TryGetValue<long>(propName, out long currentLong) && currentLong != default)
            return currentLong;

        if (change.OriginalValues.TryGetValue<long>(propName, out long originalLong) && originalLong != default)
            return originalLong;

        if (change.CurrentValues.TryGetValue<long?>(propName, out long? currentNullable) && currentNullable.HasValue)
            return currentNullable;

        if (change.OriginalValues.TryGetValue<long?>(propName, out long? originalNullable) && originalNullable.HasValue)
            return originalNullable;

        return null;
    }

    public static Guid GetGuidFromProperty(this EntityEntry change, string propName, Guid defaultValue = default)
    {
        Guid? value = GetNullableGuidFromProperty(change, propName);
        return value ?? defaultValue;
    }

    public static Guid? GetNullableGuidFromProperty(this EntityEntry change, string propName)
    {
        if (change.CurrentValues.TryGetValue<Guid>(propName, out Guid currentGuid) && currentGuid != default)
            return currentGuid;

        if (change.OriginalValues.TryGetValue<Guid>(propName, out Guid originalGuid) && originalGuid != default)
            return originalGuid;

        if (change.CurrentValues.TryGetValue<Guid?>(propName, out Guid? currentNullable) && currentNullable.HasValue)
            return currentNullable;

        if (change.OriginalValues.TryGetValue<Guid?>(propName, out Guid? originalNullable) && originalNullable.HasValue)
            return originalNullable;

        return null;
    }
}

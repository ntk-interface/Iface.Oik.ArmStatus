using System;

namespace Iface.Oik.ArmStatus.Util;

public static class OptionsGuard
{
    public static void ThrowIfNull(object value, string name)
    {
        if (value is null)
        {
            throw new Exception($"Не задан обязательный параметр \"{name}\"");
        }
    }

    public static void ThrowIfNullOrEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new Exception($"Не задан обязательный параметр \"{name}\"");
        }
    }
}

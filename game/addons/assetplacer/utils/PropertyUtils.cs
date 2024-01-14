// PropertyUtils.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

using System;

namespace AssetPlacer;

public static class PropertyUtils
{
    public static string EnumToPropertyHintString<TEnum>() where TEnum : struct, Enum
    {
        var hintString = "";
        var enumvals = Enum.GetValues<TEnum>();
        for (var i = 0; i < enumvals.Length; i++)
        {
            var enumVal = enumvals[i];
            object val = Convert.ChangeType(enumVal, enumVal.GetTypeCode());
            hintString += $"{(i==0?"":",")}{Enum.GetName(enumVal)}:{val}";
        }

        return hintString;
    }
    public static string[] EnumToStrings<TEnum>() where TEnum : struct, Enum
    {
        var enumvals = Enum.GetValues<TEnum>();
        var array = new string[enumvals.Length];
        for (var i = 0; i < enumvals.Length; i++)
        {
            var enumVal = enumvals[i];
            array[i] = Enum.GetName(enumVal)!;
        }

        return array;
    }
}
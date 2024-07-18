using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GitUpdaterConsole;
internal static class ExtensionHelper
{
    public static string GetAppSetting(this IConfiguration config, string key, string defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key)
    {
        return config.GetAppSetting(key, string.Empty);
    }
    public static string GetAppSetting(this IConfiguration config, string key, int defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, bool defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, double defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, float defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, decimal defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, long defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, short defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, byte defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, char defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, DateTime defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, TimeSpan defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetAppSetting(this IConfiguration config, string key, Guid defaultValue)
    {
        return string.IsNullOrEmpty(config[key]) ? defaultValue.ToString() : config[key]!;
    }
    public static string GetLastDirectory(this string path)
    {
        return path.Split(Path.PathSeparator).Last();
    }

    public static bool IsNullOrWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
    public static int SafeLentgh(this string? str)
    {
        return str is null ? 0 : str.Length;
    }
}

#region Copyright

// ===============================================================================
//   Project Name        :    PxeServices
//   Project Description :
//   ===============================================================================
//   File Name           :    GlobalSetting.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-10-16 11:40
//   Update Time         :    2025-10-16 11:40
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================

#endregion

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace PxeServices.Entities.Settings;

public class ObjectSetting : Entity<Guid>
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(8192)]
    public string Value { get; set; } = string.Empty;

   public T Get<T>()
    {
        // 如果值为空，返回默认
        if (string.IsNullOrWhiteSpace(Value))
            return default!;

        var targetType     = typeof(T);
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable     = Nullable.GetUnderlyingType(targetType) != null;

        // 字符串直接返回（Value 已保证非 null）
        if (underlyingType == typeof(string))
            return (T)(object)(Value ?? string.Empty);

        try
        {
            object converted;

            // IPAddress 特殊处理（优先）
            if (underlyingType == typeof(IPAddress))
            {
                if (IPAddress.TryParse(Value, out var ip))
                    converted = ip!;
                else
                    throw new FormatException($"无法将值解析为 IPAddress: '{Value}'");
            }
            // 枚举
            else if (underlyingType.IsEnum)
            {
                converted = Enum.Parse(underlyingType, Value, ignoreCase: true);
            }
            // Guid
            else if (underlyingType == typeof(Guid))
            {
                converted = Guid.Parse(Value);
            }
            // 布尔
            else if (underlyingType == typeof(bool))
            {
                // 支持 1/0, true/false, True/False
                if (int.TryParse(Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
                    converted = intVal != 0;
                else
                    converted = bool.Parse(Value);
            }
            // DateTime
            else if (underlyingType == typeof(DateTime))
            {
                converted = DateTime.Parse(Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeLocal);
            }
            // 数字和其它可由 Convert.ChangeType 处理的原始类型（包括 decimal）
            else if (underlyingType.IsPrimitive || underlyingType == typeof(decimal))
            {
                converted = Convert.ChangeType(Value, underlyingType, CultureInfo.InvariantCulture)!;
            }
            // 其它复杂类型：使用 JSON 反序列化（使用带 IPAddress 转换器的 options）
            else
            {
                try
                {
                    // 优先按 T 反序列化（支持 Nullable<T>、引用类型等）
                    var deserialized = JsonSerializer.Deserialize<T>(Value, IPAddressJsonConverter.jsonOptions);
                    if (deserialized is not null)
                        return deserialized;

                    converted = JsonSerializer.Deserialize(Value, underlyingType, IPAddressJsonConverter.jsonOptions) ?? throw new JsonException("Deserialized value was null");
                }
                catch (JsonException)
                {
                    // JSON 反序列化失败，向外抛出
                    throw;
                }
            }

            // 如果目标是 Nullable<T>，需要构造 Nullable 包装
            if (isNullable && (underlyingType.IsValueType))
            {
                var nullableType  = typeof(Nullable<>).MakeGenericType(underlyingType);
                var boxedNullable = Activator.CreateInstance(nullableType, converted);
                return (T)boxedNullable!;
            }

            return (T)converted!;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"无法将设置值转换为目标类型: Name='{Name}', Value='{Value}', TargetType='{typeof(T)}'.", ex);
        }
    }

    public T Set<T>(T value)
    {
        var targetType     = typeof(T);
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable     = Nullable.GetUnderlyingType(targetType) != null;

        // 如果 value 为 null（引用类型或可空类型），将存为空字符串
        if (value is null)
        {
            Value = string.Empty;
            return value;
        }

        // 字符串直接赋值（安全转换）
        if (underlyingType == typeof(string))
        {
            Value = value as string ?? string.Empty;
            return value;
        }

        try
        {
            // IPAddress
            if (underlyingType == typeof(IPAddress))
            {
                Value = (value as IPAddress)?.ToString() ?? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return value;
            }

            // 枚举：存名称
            if (underlyingType.IsEnum)
            {
                var name = Enum.GetName(underlyingType, value!);
                Value = name ?? string.Empty;
                return value;
            }

            // Guid
            if (underlyingType == typeof(Guid))
            {
                Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return value;
            }

            // 布尔
            if (underlyingType == typeof(bool))
            {
                // 保持小写 true/false
                Value = Convert.ToString(value, CultureInfo.InvariantCulture)?.ToLowerInvariant() ?? string.Empty;
                return value;
            }

            // DateTime
            if (underlyingType == typeof(DateTime))
            {
                if (value is DateTime dt)
                {
                    Value = dt.ToString("o", CultureInfo.InvariantCulture); // round-trip
                }
                else
                {
                    Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                }

                return value;
            }

            // 数字和其它可由 Convert.ChangeType 处理的原始类型（包括 decimal）
            if (underlyingType.IsPrimitive || underlyingType == typeof(decimal))
            {
                Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return value;
            }

            // 其它复杂类型：使用 JSON 序列化（使用带 IPAddress 转换器的 options）
            try
            {
                var serialized = JsonSerializer.Serialize(value, IPAddressJsonConverter.jsonOptions);
                Value = serialized ?? string.Empty;
                return value;
            }
            catch (JsonException)
            {
                throw;
            }
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"无法将设置值从目标类型转换为字符串: Name='{Name}', TargetType='{typeof(T)}'.", ex);
        }
    }

    #region Overrides of Entity<Guid>

    public override Guid Id { get; set; } = Guid.NewGuid();

    #endregion

}

// 自定义 IPAddress JsonConverter：以字符串形式序列化/反序列化 IPAddress，避免默认访问 ScopeId 导致异常
#region Copyright

// ===============================================================================
//   Project Name        :    PxeServices
//   Project Description :
//   ===============================================================================
//   File Name           :    IPAddressJsonConverter.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-10-16 17:36
//   Update Time         :    2025-10-16 17:36
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================

#endregion

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PxeServices.Entities.Settings;

public sealed class IPAddressJsonConverter : JsonConverter<IPAddress>
{
    // 公用 JsonSerializerOptions，包含 IPAddress 转换器，避免默认反射访问 ScopeId 导致异常
    public static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static IPAddressJsonConverter()
    {
        jsonOptions.Converters.Add(new IPAddressJsonConverter());
        // 若需要序列化枚举为字符串，可在此加入 JsonStringEnumConverter
        // _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }
    public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s))
            return IPAddress.None;
        if (IPAddress.TryParse(s, out var ip))
            return ip;
        throw new JsonException($"无法将字符串解析为 IPAddress: '{s}'");
    }

    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
    {
        // 避免访问可能抛异常的 ScopeId 等属性，仅使用 ToString()
        writer.WriteStringValue(value?.ToString() ?? string.Empty);
    }
}
﻿// ------------------------------------------------------------------------
// This file is licensed to you under the MIT License.
// ------------------------------------------------------------------------

namespace FluentPxeServer.Components.Cookies;

public class CookieState(bool? acceptAnalysis, bool? acceptSocialMedia, bool? acceptAdvertising)
{
    public bool? AcceptAnalytics { get; set; } = acceptAnalysis;
    public bool? AcceptSocialMedia { get; set; } = acceptSocialMedia;
    public bool? AcceptAdvertising { get; set; } = acceptAdvertising;

    public CookieState() : this(null, null, null)
    {
    }

    public CookieState(bool acceptAll) : this(acceptAll, acceptAll, acceptAll)
    {
    }
}

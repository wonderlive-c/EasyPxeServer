#region Copyright

// ===============================================================================
//   Project Name        :    FluentPxeServer
//   Project Description :
//   ===============================================================================
//   File Name           :    DhcpController.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-10-16 17:26
//   Update Time         :    2025-10-16 17:26
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================

#endregion

using Microsoft.AspNetCore.Mvc;
using PxeServices.Entities.Dhcp;
using PxeServices.Entities.Settings;

namespace FluentPxeServer.Controllers;

[Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
[ApiController]
public class DhcpController(IServiceProvider serviceProvider) : ControllerBase
{
    private IObjectSettingRepository SettingRepository => serviceProvider.GetService<IObjectSettingRepository>();

    // GET api/Dhcp
    [HttpGet]
    public Task<DhcpSetting?> Get() { return SettingRepository.GetObjectSettingAsync<DhcpSetting>(); }
}
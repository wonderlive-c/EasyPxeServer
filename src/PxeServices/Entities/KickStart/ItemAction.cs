#region Copyright

// ===============================================================================
//   Project Name        :    PxeServices
//   Project Description :
//   ===============================================================================
//   File Name           :    ItemAction.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-10-16 11:20
//   Update Time         :    2025-10-16 11:20
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================

#endregion

namespace PxeServices.Entities.KickStart;

public class ItemAction : Entity<Guid>
{
    public string Name        { get; set; }
    public string Description { get; set; }
    public string Command     { get; set; }
    public string InitrdUrl   { get; set; }
    public string InitrdFile  { get; set; }
    public string IpMode      { get; set; }
    public string Chain       { get; set; }
    public string InstallRepo { get; set; }
    public string ksdevice    { get; set; }

    public bool VncEnable { get; set; }

    public string InstallKs { get; set; }

    //ksdevice
    //ksdevice=link inst.vnc inst.ks=http://${next-server}/ts/tencent-mini.cfg 
}
#region Copyright

// ===============================================================================
//   Project Name        :    PxeServices
//   Project Description :
//   ===============================================================================
//   File Name           :    KickStartCfg.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-10-16 11:38
//   Update Time         :    2025-10-16 11:38
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================

#endregion

namespace PxeServices.Entities.KickStart;

public class KickStartCfg : Entity<Guid>
{
    public string Name        { get; set; }
    public string Description { get; set; }
    public string Content     { get; set; }

    public string LocalFile { get; set; }
/*
 *
   #platform=x86_64
   #version=TencentOS Server 4.4 (或对应版本)
   #install

   # 安装源配置（使用本地ISO镜像，Tencent OS 最小化安装依赖ISO自带源）
   # url --url=http://10.10.10.254:5000/api/tftpfile/download/ts/all

   # 语言与键盘
   lang en_US.UTF-8
   keyboard us

   # 网络配置（开机启动，DHCP自动获取，根据实际网卡名调整，如eth0、ens33等）
   network --onboot yes --device eth0 --bootproto dhcp --hostname tencentos-minimal

   # 根密码（示例为加密密码，实际使用需用 grub-crypt 生成，格式：$6$...）
   #rootpw --iscrypted $6$EXAMPLE_HASH$V5K8QZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZJZ

   # 认证策略
   # auth --useshadow --passalgo=sha512

   # SELinux 配置（默认启用）
   selinux --enforcing

   # 防火墙（仅开放SSH）
   firewall --service=ssh

   # 安装模式（文本模式，最小化安装无需图形界面）
   text

   # 安装完成后重启
   reboot

   # 时区设置（上海）
   timezone Asia/Shanghai --isUtc

   # 磁盘分区（MBR格式，LVM最小化方案，根据磁盘大小调整）
   ignoredisk --only-use=sda
   clearpart --all --initlabel  # 清除所有分区并初始化磁盘标签
   part /boot --fstype xfs --size=500  # /boot分区500MB
   part pv.01 --size=1 --grow  # 剩余空间作为LVM物理卷
   volgroup osvg pv.01  # 卷组命名为osvg
   logvol / --fstype xfs --name=root --vgname=osvg --size=10240  # 根分区10GB
   logvol swap --fstype swap --name=swap --vgname=osvg --size=2048  # swap分区2GB
   # 如需保留剩余空间，可删除以下行；如需分配给/home，保留此行
   logvol /home --fstype xfs --name=home --vgname=osvg --size=1 --grow

   # 最小化安装包选择（仅保留核心组件，Tencent OS 包组与CentOS类似）
   %packages
   @core  # 核心包组（最小化必备）
   chrony  # 时间同步工具
   vim-minimal  # 基础vim编辑器
   openssh-server  # SSH服务（远程连接必备）
   net-tools  # 网络工具（ifconfig等）
   %end

   # 安装后脚本（基础配置）
   %post
   # 启用并启动SSH服务
   systemctl enable --now sshd

   # 禁用首次登录强制改密码（最小化安装可选）
   sed -i 's/^PasswordAuthentication yes/PasswordAuthentication no/' /etc/ssh/sshd_config  # 如需密码登录可注释此行
   sed -i 's/^#PermitRootLogin yes/PermitRootLogin yes/' /etc/ssh/sshd_config  # 允许root远程登录（按需调整）
   systemctl restart sshd

   # 清理yum缓存（使用ISO源）
   yum clean all
   %end
 */
}
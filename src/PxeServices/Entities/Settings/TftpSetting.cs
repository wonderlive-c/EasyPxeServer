namespace PxeServices.Entities.Settings;

public class TftpSetting
{
    public static readonly TftpSetting Default = new()
    {
        TftpEnabled = true, TftpShareFolder = "tftpboot", Port = 69
    };

    public bool   TftpEnabled     { get; set; }
    public string TftpServerUrl   { get; set; }
    public string TftpShareFolder { get; set; }
    public int    Port            { get; set; }
}
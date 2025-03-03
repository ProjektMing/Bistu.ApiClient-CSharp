using System.ComponentModel;

namespace Bistu.Api.Models.QrCode;

public enum LoginStatus
{
    [Description("已请求")] Requested = 0,
    [Description("已扫描")] Scanned = 2,
    [Description("成功")] Success = 1,
    [Description("已过期")] Expired = 3
}

# Bistu.Api

> 本包是为 BISTU 校园网制作的第三方 API。
>
> 通过爬取 BISTU 校园的数据，提供结构化的数据接口，以便 .NET 开发者更方便地获取校园信息。
> API 仍在开发中，且将于 2027 年夏停止更新。（因为那时候我就毕业了）
> 如果您有任何问题或建议，请通过电子邮件与我联系：2023011211@bistu.edu.cn

## 使用方法

### 1. 安装 NuGet 包

```shell
dotnet add package Bistu.Api # 暂未发布
```

### 2. 基本用法

```csharp
using Bistu.Api;

// 创建客户端实例
using var client = new BistuClient();
```

### 3. 认证方式

#### 用户名密码认证

```csharp
using var client = new BistuClient();

// 配置用户名密码认证
bool success = await client
    .UsePassword("your_username", "your_password")
    .LoginAsync();

if (success)
{
    Console.WriteLine("登录成功！");
    // 进行其他操作...
}
```

#### 二维码认证

```csharp
using var client = new BistuClient();

// 配置二维码认证
bool success = await client
    .UseQrCode(qrCodeUrl => 
    {
        Console.WriteLine($"请扫描二维码登录: {qrCodeUrl}");
        // 可以在这里打开浏览器显示二维码
        Process.Start(new ProcessStartInfo(qrCodeUrl) { UseShellExecute = true });
    })
    .LoginAsync();

if (success)
{
    Console.WriteLine("登录成功！");
    // 进行其他操作...
}
```

### 4. 完整示例

```csharp
using Bistu.Api;
using Microsoft.Extensions.Logging;

// 创建日志记录器
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

try
{
    using var client = new BistuClient();
    
    // 方式1：用户名密码登录
    var passwordSuccess = await client
        .UsePassword("your_username", "your_password")
        .LoginAsync();
    
    // 方式2：二维码登录
    var qrSuccess = await client
        .UseQrCode(qrUrl => logger.LogInformation("二维码地址: {QrUrl}", qrUrl))
        .LoginAsync();
    
    if (passwordSuccess || qrSuccess)
    {
        logger.LogInformation("认证成功，可以访问校园系统了！");
        
        // Cookie 会自动维护，可以继续进行其他 API 调用
        var cookies = client.CookieContainer.GetCookies(client.PortalAddress);
        logger.LogInformation("获得 {Count} 个 cookies", cookies.Count);
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "认证失败");
}
```

## 特性

### ✨ 主要功能

- **🔐 多种认证方式**: 支持用户名密码和二维码两种登录方式
- **🍪 自动会话管理**: 自动处理 Cookie 和会话状态
- **🛠️ 链式调用**: 支持流畅的方法链式调用
- **♻️ 资源管理**: 实现 `IDisposable`，自动释放资源
- **📊 灵活配置**: 可配置 CAS 服务器和门户地址

### 🔧 技术特性

- **.NET 10** 支持
- **C# 13** 语法特性
- **异步编程** 全面支持
- **完整的错误处理** 和异常信息
- **详细的 XML 文档** 注释

## API 列表

### 认证相关

- `UsePassword(string username, string password)`: 配置用户名密码认证
- `UseQrCode(Action<string> qrCodeHandler)`: 配置二维码认证
- `LoginAsync()`: 执行登录操作

### 配置属性

- `CasAddress`: CAS 认证服务器地址
- `PortalAddress`: 教务系统门户地址
- `CookieContainer`: Cookie 容器，用于会话管理

### 即将支持

- `GetNewsAsync()`: 获取校园新闻
- `GetGradeAsync()`: 获取成绩
- `GetScheduleAsync()`: 获取课表
- `GetExamAsync()`: 获取考试安排

## 项目地址

- **GitHub**: https://github.com/ProjektMing/Bistu.ApiClient-CSharp
- **NuGet**: 即将发布

## 开源协议

Apache-2.0

## 作者

- **ProjektMing** - 初始开发者

## 贡献

欢迎提交 Issue 和 Pull Request！

## 特别感谢

感谢 BISTU 提供的技术环境和学习平台。

## 更新日志

### v0.0.1 (开发中)

- ✅ 实现基础认证功能
- ✅ 支持用户名密码认证
- ✅ 支持二维码认证
- ✅ 自动会话管理
- ✅ 完整的资源释放
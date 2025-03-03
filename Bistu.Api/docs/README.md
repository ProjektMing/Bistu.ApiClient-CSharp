# Bistu.Api

> 本包是为 BISTU 校园网制作的第三方 API。
>
> 通过爬取 BISTU 校园的数据，提供结构化的数据接口，以便 .NET 开发者更方便地获取校园信息。
> API 仍在开发中，且将于 2027 年夏停止更新。（因为那时候我就毕业了）
> 如果您有任何问题或建议，请通过电子邮件与我联系：2023011211@bistu.edu.cn

## 使用方法

1. 安装 NuGet 包

```shell
dotnet add package Bistu.Api # 暂未发布
```

2. 创建一个 `BistuClient` 实例

```csharp
var client = new BistuClient();
```

3. 调用方法获取数据

```csharp
var news = await client.GetNewsAsync(); # 暂不提供该方法
```

## API 列表

- `GetNewsAsync()`: 获取校园新闻
- `AuthenticateAsync(string username, string password)`: 校园网认证
- `GetGradeAsync()`: 获取成绩
- `GetScheduleAsync()`: 获取课表
- `GetExamAsync()`: 获取考试安排

等，暂未列全。

## 项目地址

- GitHub: https://github.com/ProjektMing/Bistu.ApiClient-CSharp
- NuGet:

## 开源协议

Apache-2.0

## 作者

- ProjektMing

## 贡献者

- ProjektMing

## 特别感谢

## 更新日志

空
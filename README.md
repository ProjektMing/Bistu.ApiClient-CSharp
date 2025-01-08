# Bistu.Api
灵感来源于学长制作的[金智教务网登录程序](https://github.com/Bistutu/GoCampusLogin),尝试使用 C# 重实现。
目前仅实现了获取登录 cookie 的功能

路线图：
- [ ] 完整的 log
- [ ] 修补部分异常处理
- [ ] 更改为nupkg打包
- [ ] 充分的注释

当前流程：运行后会在终端输出两行网址，第一行为二维码。第二行为二维码指向的地址，但目前仅有第一行可正常使用，扫码等待，会打印登录所需的 cookie。

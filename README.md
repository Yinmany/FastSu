# FastSu

![Static Badge](https://img.shields.io/badge/version-0.0.0--beta-red)

使用`C#`构建的一个轻快`服务器框架`

* `Actor`模型
* 全逻辑不停服热更(`Full Reload`)
* 网络层是`AspNetCore`的`Kestrel`支持`Tcp`、`Http`、`WebSocket`、`Quic`
    * 其它可靠`Udp`自行接入即可，如: [kcp2k](https://github.com/MirrorNetworking/kcp2k)、[LiteNetLib](https://github.com/RevenantX/LiteNetLib)
* 适用于`任何游戏类型`或其它`即时类应用`服务端程序
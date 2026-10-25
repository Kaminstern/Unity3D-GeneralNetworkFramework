proto修改或添加字段后，运行`ProtobufTool\scripts\gen_all.bat`生成对应cs代码并拷贝到客户端服务端（应该能只是用一份，后面看看需不需要统一一个Common来存放）

客户端socket喜欢命名为clientfd，这个fd指文件描述符，file descriptor的缩写

设置了缓冲区长度1024，单条协议超过缓冲区长度时会发生错误，根据需要调整

### 已经改造

没有用protobuf-net，直接用的Google protobuf

服务端接收不用多路复用，每个连接直接异步接收

服务端不用同步发送，每个连接直接异步发送

服务端通过构建一个GetTimeStamp来获取时间戳，直接使用Environment.TickCount64;返回的是自系统启动以来所经过的毫秒数

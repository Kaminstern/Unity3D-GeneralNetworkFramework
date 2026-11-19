proto修改或添加字段后，运行`ProtobufTool\scripts\gen_all.bat`生成对应cs代码并拷贝到客户端服务端（应该能只是用一份，后面看看需不需要统一一个Common来存放）

客户端socket喜欢命名为clientfd，这个fd指文件描述符，file descriptor的缩写

设置了缓冲区长度1024，单条协议超过缓冲区长度时会发生错误，根据需要调整

传输协议的格式从高到低位是

1、两字节，表示bodyLength，这个bodyLength只表示后面内容的长度，不计算这两个字节

2、两字节，表示msgLength，表示协议名长度，不计算这两个字节

3、msgLength，协议名

4、剩下的，表示具体的消息，长度位bodyLength - nameCount，其中nameCount = 2 + msgLength

### 已经改造

没有用protobuf-net，直接用的Google protobuf

服务端接收不用多路复用，每个连接直接异步接收

服务端不用同步发送，每个连接直接异步发送

服务端通过构建一个GetTimeStamp来获取时间戳，直接使用Environment.TickCount64;返回的是自系统启动以来所经过的毫秒数

没有用MySql.Data，直接用MySqlConnector，性能更高，内存占用更低，从底层为 **`async/await`** 设计 ，提供真正、可取消的异步 I/O，可靠性高，兼容性更广

服务端DbManager中的Java序列化，改成了官方的`JsonSerializer`

using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace GeneralClientFramework
{
    // 所有协议类的基类
    public class MsgBase
    {
        // 协议名
        public string protoName = "";

        // 协议名 -> Parser 的映射表
        private static readonly Dictionary<string, MessageParser> parsers = new Dictionary<string, MessageParser>();

        // 注册单个协议（初始化时调用）
        public static void Register<T>(MessageParser<T> parser)
             where T : IMessage<T>, new() 
        {
            // 通过new T() 拿到实例，再取 Descriptor.FullName
            string name = new T().Descriptor.FullName;
            parsers[name] = parser;
        }

        // 编码
        public static byte[] Encode(IMessage msg)
        {
            return msg.ToByteArray();
        }

        // 解码
        public static IMessage Decode(string protoName, byte[] bytes, int offset, int count)
        {
            if(!parsers.TryGetValue(protoName, out var parser))
            {
                throw new Exception($"未注册协议： {protoName}");
            }
            return parser.ParseFrom(bytes, offset, count);
        }

        // 编码协议名（2字节长度+字符串）
        public static byte[] EncodeName(IMessage msg)
        {
            // 名字bytes和长度
            string name = msg.Descriptor.FullName;
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            Int16 len = (Int16)nameBytes.Length;
            // 申请bytes数值
            byte[] bytes = new byte[2 + len];
            // 组装2字节的长度信息（小端存储，先存低位，再存高位）书上的示意图标注的是大端存储
            bytes[0] = (byte)(len % 256);
            bytes[1] = (byte)(len / 256);
            // 组装名字bytes
            Array.Copy(nameBytes, 0, bytes, 2, len);
            return bytes;
        }

        // 解析协议名
        public static string DecodeName(byte[] bytes, int offset, out int count)
        {
            count = 0;
            // 必须大于2个字节
            if (offset + 2 > bytes.Length)
            {
                return "";
            }
            // 读取长度
            Int16 len = (Int16)(bytes[offset] | (bytes[offset + 1] << 8));
            if (len <= 0)
            {
                return "";
            }
            if (offset + 2 + len > bytes.Length)
            {
                return "";
            }
            // 解析
            count = 2 + len;  // 协议名信息的字节数，包括前面计数的2个字节和协议名所占字节
            string name = System.Text.Encoding.UTF8.GetString(bytes, offset + 2, len);
            return name;
        }

        // 打包成 [2字节长度名][协议名][消息体]
        public static byte[] Pack(IMessage msg)
        {
            byte[] nameBytes = EncodeName(msg);
            byte[] bodyBytes = Encode(msg);

            byte[] full = new byte[nameBytes.Length + bodyBytes.Length];
            Array.Copy(nameBytes, 0, full, 0, nameBytes.Length);
            Array.Copy(bodyBytes, 0, full, nameBytes.Length, bodyBytes.Length);
            return full;
        }
    }
}
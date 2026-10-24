
namespace GeneralServerFramework
{
    public class ByteArray
    {
        // 默认大小
        const int DEFAULT_SIZE = 1024;
        // 初始大小
        int initSize = 0;
        // 缓冲区
        public byte[] bytes;
        // 读写位置
        public int readIdx = 0;
        public int writeIdx = 0;
        // 容量
        private int capacity = 0;
        // 剩余空间
        public int remain
        {
            get
            {
                return capacity - writeIdx;
            }
        }
        // 存储数据长度
        public int length
        {
            get
            {
                return writeIdx - readIdx;
            }
        }

        public ByteArray(byte[] defaultBytes)
        {
            bytes = defaultBytes;
            capacity = defaultBytes.Length;
            initSize = defaultBytes.Length;
            readIdx = 0;
            writeIdx = defaultBytes.Length;
        }

        public ByteArray(int size = DEFAULT_SIZE)
        {
            bytes = new byte[size];
            capacity = size;
            initSize = size;
            readIdx = 0;
            writeIdx = 0;
        }

        // 重设尺寸
        public void ReSize(int size)        // size表示所需数据长度
        {
            if (size < length || size < initSize)
            {
                return;
            }
            int n = 1;
            while (n < size)
            {
                n *= 2;
            }
            capacity = n;
            byte[] newBytes = new byte[capacity];
            Array.Copy(bytes, readIdx, newBytes, 0, writeIdx - readIdx);
            bytes = newBytes;
            writeIdx = length;
            readIdx = 0;
        }

        // 检查并移动数据
        public void CheckAndMoveBytes()
        {
            if (length < 8)
            {
                MoveBytes();
            }
        }

        // 移动数据
        public void MoveBytes()
        {
            if (length > 0)
            {
                Array.Copy(bytes, readIdx, bytes, 0, length);
            }
            writeIdx = length;
            readIdx = 0;
        }

        // 写入数据
        public int Write(byte[] bs, int offset, int count)
        {
            if (remain < count)
            {
                ReSize(length + count);
            }
            Array.Copy(bs, offset, bytes, writeIdx, count);
            writeIdx += count;
            return count;
        }

        // 读取数据
        public int Read(byte[] bs, int offset, int count)
        {
            count = Math.Min(count, length);
            Array.Copy(bytes, readIdx, bs, offset, count);
            readIdx += count;
            CheckAndMoveBytes();
            return count;
        }

        // 读取Int16
        public Int16 ReadInt16()
        {
            if (length < 2)
            {
                return 0;
            }
            Int16 ret = (Int16)(bytes[readIdx] | (bytes[readIdx + 1] << 8));
            readIdx += 2;
            CheckAndMoveBytes();
            return ret;
        }

        // 读取Int32
        public Int32 ReadInt32()
        {
            if (length < 4)
            {
                return 0;
            }
            Int32 ret = (Int32)(bytes[readIdx] |
                                (bytes[readIdx + 1] << 8) |
                                (bytes[readIdx + 2] << 16) |
                                (bytes[readIdx + 3] << 24));
            readIdx += 4;
            CheckAndMoveBytes();
            return ret;
        }

        // 打印缓冲区
        public override string ToString()
        {
            return BitConverter.ToString(bytes, readIdx, length);
        }

        // 打印调试信息
        public string Debug()
        {
            return $"readIdx({readIdx}) writeIdx({writeIdx}) bytes({BitConverter.ToString(bytes, 0, bytes.Length)})";
        }
    }
}
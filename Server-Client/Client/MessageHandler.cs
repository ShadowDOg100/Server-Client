using System;
using System.Text;

public class Message
{
    public string message;
    public byte[] file;
};

class MessageHandler
{
    public static Message getMessage(byte[] data)
    {
        Message m = new Message();

        byte[] file = null;
        byte[] message = new byte[data[4]];

        Buffer.BlockCopy(data, 5, message, 0, data[4]);

        if (data.Length > message.Length + 5)
        {
            byte[] length = new byte[4];
            Buffer.BlockCopy(data, message.Length + 5, length, 0, 4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(length);
            int l = BitConverter.ToInt32(length, 0);
            file = new byte[l];
            Buffer.BlockCopy(data, message.Length + 9, file, 0, l);
        }

        m.message = Encoding.ASCII.GetString(message);
        m.file = file;

        return m;
    }

    public static byte[] getByteMessage(string message, byte[] bytes)
    {
        byte[] messageData = Encoding.ASCII.GetBytes(message);
        byte[] ret = new byte[messageData.Length + 5];
        Buffer.BlockCopy(messageData, 0, ret, 5, messageData.Length);
        if (bytes != null)
        {
            ret = new byte[messageData.Length + bytes.Length + 9];
            Buffer.BlockCopy(messageData, 0, ret, 5, messageData.Length);
            Buffer.BlockCopy(bytes, 0, ret, messageData.Length + 9, bytes.Length);

            ret[messageData.Length + 5] = (byte)(bytes.Length >> 24);
            ret[messageData.Length + 6] = (byte)(bytes.Length >> 16);
            ret[messageData.Length + 7] = (byte)(bytes.Length >> 8);
            ret[messageData.Length + 8] = (byte)bytes.Length;
        }

        ret[0] = (byte)(ret.Length >> 24);
        ret[1] = (byte)(ret.Length >> 16);
        ret[2] = (byte)(ret.Length >> 8);
        ret[3] = (byte)ret.Length;
        ret[4] = (byte)(messageData.Length);

        return ret;
    }
};
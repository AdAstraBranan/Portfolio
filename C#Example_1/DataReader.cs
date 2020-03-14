using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class DataReader
{
    public MemoryStream Internal;

    public DataReader(MemoryStream stream)
    {
        Internal = stream;
    }

    public byte ReadByte()
    {
        int r = Internal.ReadByte();
        if (r < 0)
        {
            throw new EndOfStreamException("Failed to read from stream, " + Internal.Length + " bytes were available (now none)...");
        }
        return (byte)r;
    }

    public int Available
    {
        get
        {
            return (int)(Internal.Length - Internal.Position);
        }
    }

    private byte[] ReadBytes(int length)
    {
        byte[] bytes = new byte[length];
        for (int i = 0; i < length; i++)
        {
            bytes[i] = ReadByte();
        }
        return bytes;
    }

    public byte[] ReadFullBytes()
    {
        int length = ReadInt();
        return ReadBytes(length);
    }

    public short ReadShort()
    {
        return BitConverter.ToInt16(ReadBytes(2), 0);
    }

    public int ReadInt()
    {
        return BitConverter.ToInt32(ReadBytes(4), 0);
    }

    public long ReadLong()
    {
        return BitConverter.ToInt64(ReadBytes(8), 0);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class DataWriter
{
    public MemoryStream Internal;

    public DataWriter(MemoryStream stream)
    {
        Internal = stream;
    }

    public void WriteFullBytes(byte[] bytes)
    {
        WriteInt(bytes.Length);
        Internal.Write(bytes, 0, bytes.Length);
    }

    public void WriteShort(short num)
    {
        Internal.Write(BitConverter.GetBytes(num), 0, 2);
    }

    public void WriteInt(int num)
    {
        Internal.Write(BitConverter.GetBytes(num), 0, 4);
    }

    public void WriteLong(long num)
    {
        Internal.Write(BitConverter.GetBytes(num), 0, 8);
    }
}

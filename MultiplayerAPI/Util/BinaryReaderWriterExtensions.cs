using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPAPI.Util;

public static class BinaryReaderWriterExtensions
{
    public static void WriteUShortArray(this BinaryWriter writer, ushort[] array)
    {
        if (array == null)
        {
            writer.Write(0);
            return;
        }
        writer.Write(array.Length);
        foreach (ushort value in array)
        {
            writer.Write(value);
        }
    }
    
    public static void WriteInt32Array(this BinaryWriter writer, int[] array)
    {
        if (array == null)
        {
            writer.Write(0);
            return;
        }
        writer.Write(array.Length);
        foreach (int value in array)
        {
            writer.Write(value);
        }
    }

    public static ushort[] ReadUShortArray(this BinaryReader reader)
    {
        var length = reader.ReadInt32();

        var ret = new ushort[length];
        for (int i = 0; i < length; i++)
        {
            ret[i] = reader.ReadUInt16();
        }
        return ret;
    }

    public static int[] ReadInt32Array(this BinaryReader reader)
    {
        var length = reader.ReadInt32();

        var ret = new int[length];
        for (int i = 0; i < length; i++)
        {
            ret[i] = reader.ReadInt32();
        }
        return ret;
    }
}

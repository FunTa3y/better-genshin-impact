using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BetterGenshinImpact.Helpers;

public class ObjectUtils
{
    [Obsolete("Obsolete")]
    public static byte[] Serialize(object obj)
    {
        var ms = new MemoryStream();
        var formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011
        formatter.Serialize(ms, obj);
#pragma warning restore SYSLIB0011
        byte[] bytes = ms.GetBuffer();
        return bytes;
    }

    [Obsolete("Obsolete")]
    public static object Deserialize(byte[] bytes)
    {
        //Используйте переданныйbyte[]Создать поток памяти
        var ms = new MemoryStream(bytes)
        {
            Position = 0
        };
        var formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011
        var obj = formatter.Deserialize(ms); //Десериализовать поток памяти в объект
#pragma warning restore SYSLIB0011
        ms.Close();
        return obj;
    }
}

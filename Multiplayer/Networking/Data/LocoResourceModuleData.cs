using DV.ThingTypes;
using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Multiplayer.Networking.Data;

public readonly struct LocoResourceModuleData(ResourceType resourceType, float[] values)
{
    public readonly ResourceType ResourceType = resourceType;
    public readonly float[] Values = values;

    public static LocoResourceModuleData From(LocoResourceModule resources)
    {
        //extract floats
        var values = resources.resourceData.Select(d => d.unitsToBuy).ToArray();

        return new LocoResourceModuleData(resources.resourceType, values);
    }

    public static void Serialize(NetDataWriter writer, LocoResourceModuleData data)
    {
        writer.Put((int)data.ResourceType);

        writer.Put(data.Values.Count());
        foreach (var val in data.Values)
            writer.Put(val);
    }

    public static LocoResourceModuleData Deserialize(NetDataReader reader)
    {
        var type = (ResourceType)reader.GetInt();

        var valueCount = reader.GetInt();

        float[] states = new float[valueCount];
        for (int i = 0; i < valueCount; i++)
            states[i] = reader.GetFloat();

        return new LocoResourceModuleData(type, states);
    }
}

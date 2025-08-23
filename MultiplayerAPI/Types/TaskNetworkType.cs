using DV.Logic.Job;
using System.Collections.Generic;
using System.IO;

namespace MPAPI.Types;

#region TaskData Base Class
public abstract class TaskNetworkData
{
    public TaskState State { get; set; }
    public float TaskStartTime { get; set; }
    public float TaskFinishTime { get; set; }
    public bool IsLastTask { get; set; }
    public float TimeLimit { get; set; }
    public TaskType TaskType { get; set; }

    public abstract void Serialize(BinaryWriter writer);
    public abstract void Deserialize(BinaryReader reader);
    public abstract Task ToTask();
    public abstract List<ushort> GetCars();
}

public abstract class TaskNetworkData<T> : TaskNetworkData where T : TaskNetworkData<T>
{
    public abstract T FromTask(Task task);

    protected void SerializeCommon(BinaryWriter writer)
    {
        //Multiplayer.Log($"TaskNetworkData.SerializeCommon() State {(byte)State}, {State}");
        writer.Write((byte)State);
        //Multiplayer.Log($"TaskNetworkData.SerializeCommon() TaskStartTime {TaskStartTime}");
        writer.Write(TaskStartTime);
        //Multiplayer.Log($"TaskNetworkData.SerializeCommon() TaskFinishTime {TaskFinishTime}");
        writer.Write(TaskFinishTime);
        //Multiplayer.Log($"TaskNetworkData.SerializeCommon() IsLastTask {IsLastTask}");
        writer.Write(IsLastTask);
        //Multiplayer.Log($"TaskNetworkData.SerializeCommon() TimeLimit {TimeLimit}");
        writer.Write(TimeLimit);
        //Multiplayer.Log($"TaskNetworkData.SerializeCommon() TaskType {(byte)TaskType}, {TaskType}");
        writer.Write((byte)TaskType);
    }

    protected void DeserializeCommon(BinaryReader reader)
    {
        State = (TaskState)reader.ReadByte();
        //Multiplayer.Log($"TaskNetworkData.DeserializeCommon() State {State}");
        TaskStartTime = reader.ReadSingle();
        //Multiplayer.Log($"TaskNetworkData.DeserializeCommon() TaskStartTime {TaskStartTime}");
        TaskFinishTime = reader.ReadSingle();
        //Multiplayer.Log($"TaskNetworkData.DeserializeCommon() TaskFinishTime {TaskFinishTime}");
        IsLastTask = reader.ReadBoolean();
        //Multiplayer.Log($"TaskNetworkData.DeserializeCommon() IsLastTask {IsLastTask}");
        TimeLimit = reader.ReadSingle();
        //Multiplayer.Log($"TaskNetworkData.DeserializeCommon() TimeLimit {TimeLimit}");
        TaskType = (TaskType)reader.ReadByte();
        //Multiplayer.Log($"TaskNetworkData.DeserializeCommon() TaskType {TaskType}");
    }
}

#endregion

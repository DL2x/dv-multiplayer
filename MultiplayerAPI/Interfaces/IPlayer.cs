using UnityEngine;

namespace MPAPI.Interfaces
{
    public interface IPlayer
    {
        public byte Id { get; }
        public string Username { get; set; }
        Vector3 Position { get; }
        float RotationY { get; }
        bool IsLoaded { get; }
        bool IsHost { get; }
        int Ping { get; }

        // Car information
        bool IsOnCar { get; }
        TrainCar OccupiedCar { get; }
    }
}

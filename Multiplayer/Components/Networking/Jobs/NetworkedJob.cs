using DV.Logic.Job;
using Multiplayer.Components.Networking.World;
using Multiplayer.Networking.Data;
using Multiplayer.Utils;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Multiplayer.Components.Networking.Jobs;

public class NetworkedJob : IdMonoBehaviour<ushort, NetworkedJob>
{
    #region Lookup Cache

    private static readonly Dictionary<Job, NetworkedJob> jobToNetworkedJob = new();
    private static readonly Dictionary<string, NetworkedJob> jobIdToNetworkedJob = new();
    private static readonly Dictionary<string, Job> jobIdToJob = new();

    public static bool Get(ushort netId, out NetworkedJob obj)
    {
        bool b = Get(netId, out IdMonoBehaviour<ushort, NetworkedJob> rawObj);
        obj = (NetworkedJob)rawObj;
        return b;
    }

    public static bool GetJob(ushort netId, out Job obj)
    {
        bool b = Get(netId, out NetworkedJob networkedJob);
        obj = b ? networkedJob.Job : null;
        return b;
    }

    public static bool TryGetFromJob(Job job, out NetworkedJob networkedJob)
    {
        return jobToNetworkedJob.TryGetValue(job, out networkedJob);
    }

    public static bool TryGetFromJobId(string jobId, out NetworkedJob networkedJob)
    {
        return jobIdToNetworkedJob.TryGetValue(jobId, out networkedJob);
    }

    public static bool TryGetNetId(Job job, out ushort netId)
    {
        if (TryGetFromJob(job, out var networkedJob))
        {
            netId = networkedJob.NetId;
            return true;
        }

        netId = 0;
        return false;
    }

    #endregion

    private static readonly Dictionary<Job, List<Task>> pendingJobTasks = [];

    protected override bool IsIdServerAuthoritative => true;
    public enum DirtyCause
    {
        JobOverview,
        JobBooklet,
        JobReport,
        JobState
    }

    public Job Job { get; private set; }
    public NetworkedStationController Station { get; private set; }

    private NetworkedItem _jobOverview;
    public NetworkedItem JobOverview
    {
        get => _jobOverview;
        set
        {
            if (value != null && value.GetTrackedItem<JobOverview>() == null)
                return;

            _jobOverview = value;

            if (value != null)
            {
                Cause = DirtyCause.JobOverview;
                OnJobDirty?.Invoke(this);
            }
        }
    }

    private NetworkedItem _jobBooklet;
    public NetworkedItem JobBooklet
    {
        get => _jobBooklet;
        set
        {
            if (value != null && value.GetTrackedItem<JobBooklet>() == null)
                return;

            _jobBooklet = value;
            if (value != null)
            {
                Cause = DirtyCause.JobBooklet;
                OnJobDirty?.Invoke(this);
            }
        }
    }
    private NetworkedItem _jobReport;
    public NetworkedItem JobReport
    {
        get => _jobReport;
        set
        {
            if (value != null && value.GetTrackedItem<JobReport>() == null)
                return;

            _jobReport = value;
            if (value != null)
            {
                Cause = DirtyCause.JobReport;
                OnJobDirty?.Invoke(this);
            }
        }
    }

    private List<NetworkedItem> JobReports = new List<NetworkedItem>();

    public Guid OwnedBy { get; set; } = Guid.Empty;
    public JobValidator JobValidator { get; set; }

    public bool ValidatorRequestSent { get; set; } = false;
    public bool ValidatorResponseReceived { get; set; } = false;
    public bool ValidationAccepted { get; set; } = false;
    public ValidationType ValidationType { get; set; }

    public DirtyCause Cause { get; private set; }

    public Action<NetworkedJob> OnJobDirty;

    public List<ushort> JobCars = [];

    private readonly IdPool<ushort> idPool = new();
    private readonly Dictionary<ushort, Task> taskMap = [];

    protected override void Awake()
    {
        base.Awake();
    }

    protected void Start()
    {
        if (Job != null)
        {
            AddToCache();
        }
        else
        {
            Multiplayer.LogError($"NetworkedJob Start(): Job is null for {gameObject.name}");
        }
    }

    public void Initialize(Job job, NetworkedStationController station)
    {
        Job = job;
        Station = station;

        // Setup handlers
        job.JobTaken += OnJobTaken;
        job.JobAbandoned += OnJobAbandoned;
        job.JobCompleted += OnJobCompleted;
        job.JobExpired += OnJobExpired;

        // If this is called after Start(), we need to add to cache here
        if (gameObject.activeInHierarchy)
        {
            AddToCache();
        }

        //Check for any pending tasks that were added before the NetworkedJob was created
        if(pendingJobTasks.TryGetValue(job, out var taskList) && taskList != null)
        {
            taskList.ForEach(t => AddTask(t));
            pendingJobTasks.Remove(job);
        }
    }

    private void AddToCache()
    {
        jobToNetworkedJob[Job] = this;
        jobIdToNetworkedJob[Job.ID] = this;
        jobIdToJob[Job.ID] = Job;
        //Multiplayer.Log($"NetworkedJob added to cache: {Job.ID}");
    }

    private void OnJobTaken(Job job, bool viaLoadGame)
    {
        if (viaLoadGame)
            return;

        Cause = DirtyCause.JobState;
        OnJobDirty?.Invoke(this);
    }

    private void OnJobAbandoned(Job job)
    {
        Cause = DirtyCause.JobState;
        OnJobDirty?.Invoke(this);
    }

    private void OnJobCompleted(Job job)
    {
        Cause = DirtyCause.JobState;
        OnJobDirty?.Invoke(this);
    }

    private void OnJobExpired(Job job)
    {
        Cause = DirtyCause.JobState;
        OnJobDirty?.Invoke(this);
    }

    public void AddReport(NetworkedItem item)
    {
        if (item == null || !item.UsefulItem)
        {
            Multiplayer.LogError($"Attempted to add a null or uninitialised report: JobId: {Job?.ID}, JobNetID: {NetId}");
            return;
        }

        Type reportType = item.TrackedItemType;
        if(    reportType == typeof(JobReport) ||
               reportType == typeof(JobExpiredReport) ||
               reportType == typeof(JobMissingLicenseReport) /*||
               reportType == typeof(Debtre) ||*/
           )
        {
            JobReports.Add(item);
            Cause = DirtyCause.JobReport;
            OnJobDirty?.Invoke(this);
        }
    }

    public void RemoveReport(NetworkedItem item)
    {

    }

    public void ClearReports()
    {
        foreach (var report in JobReports)
        {
            Destroy(report.gameObject);
        }

        JobReports.Clear();
    }

    protected void OnDisable()
    {
        if (UnloadWatcher.isQuitting || UnloadWatcher.isUnloading)
            return;

        // Remove from lookup caches
        jobToNetworkedJob.Remove(Job);
        jobIdToNetworkedJob.Remove(Job.ID);
        jobIdToJob.Remove(Job.ID);

        // Unsubscribe from events
        Job.JobTaken -= OnJobTaken;
        Job.JobAbandoned -= OnJobAbandoned;
        Job.JobCompleted -= OnJobCompleted;
        Job.JobExpired -= OnJobExpired;

        Destroy(this);
    }

    public static void EnqueTask(Task task)
    {
        if (!pendingJobTasks.TryGetValue(task.Job, out var taskList))
        {
            taskList = [];
            pendingJobTasks[task.Job] = taskList;
        }
        taskList.Add(task);
    }

    private void AddTask(Task task)
    {
        if (taskMap.Values.Contains(task))
            return;

        var netId = idPool.NextId;

        Multiplayer.LogDebug(() => $"NetworkedJob.AddTask() JobId: [{Job.ID}, {NetId}], TaskType: {task.InstanceTaskType}, taskNetId: {netId}");
        taskMap[netId] = task;
    }

    public void AddTask(ushort netId, Task task)
    {
        if (taskMap.Values.Contains(task))
            return;

        if (taskMap.ContainsKey(netId))
        {
            Multiplayer.LogError($"NetworkedJob.AddTask({task.InstanceTaskType}) Attempted to add a task with duplicate netId: {netId} JobId: [{Job.ID}, {NetId}]");
            return;
        }

        Multiplayer.LogDebug(() => $"NetworkedJob.AddTask({netId}, {task.InstanceTaskType}) JobId: [{Job.ID}, {NetId}]");

        taskMap[netId] = task;
    }

    public ushort GetTaskNetId(Task task)
    {
        foreach (var kvp in taskMap)
        {
            if (kvp.Value == task)
                return kvp.Key;
        }
        return 0;
    }

    public Task GetTaskFromNetId(ushort netId)
    {
        if (taskMap.TryGetValue(netId, out var task))
            return task;
        return null;
    }
}

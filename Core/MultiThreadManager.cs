using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using JM.LinqFaster;

namespace ExileCore;

/// <summary>A unit of work scheduled on a worker thread, tracking its run state and elapsed time.</summary>
[DebuggerDisplay("Name: {Name}, Elapsed: {ElapsedMs}, Completed: {IsCompleted}, Failed: {IsFailed}")]
public class Job
{
    /// <summary>Set when the job has finished (successfully or otherwise).</summary>
    public volatile bool IsCompleted;

    /// <summary>Set when the job threw an exception.</summary>
    public volatile bool IsFailed;

    /// <summary>Set when the job has been handed to a worker thread.</summary>
    public volatile bool IsStarted;

    /// <summary>Creates a named job wrapping the given work delegate.</summary>
    public Job(string name, Action work)
    {
        Name = name;
        Work = work;
    }

    /// <summary>The work to execute.</summary>
    public Action Work { get; set; }

    /// <summary>The job name (used for diagnostics).</summary>
    public string Name { get; set; }

    /// <summary>The thread the job is currently assigned to.</summary>
    public ThreadUnit WorkingOnThread { get; set; }

    /// <summary>How long the job took to run, in milliseconds.</summary>
    public double ElapsedMs { get; set; }
}

/// <summary>
/// Distributes <see cref="Job"/>s across a pool of long-lived worker threads (<see cref="ThreadUnit"/>),
/// repairing threads whose work exceeds a critical time budget.
/// </summary>
public class MultiThreadManager
{
    private const long CriticalWorkTimeMs = 750;
    private readonly object locker = new object();
    private int _lock;

    //Used for debug, maybe now can be delete
    private object _objectInitWork;
    private readonly List<ThreadUnit> BrokenThreads = new List<ThreadUnit>();
    private bool Closed;
    private readonly ConcurrentQueue<ThreadUnit> FreeThreads = new ConcurrentQueue<ThreadUnit>();
    private readonly ConcurrentQueue<Job> Jobs = new ConcurrentQueue<Job>();
    private readonly Queue<Job> processJobs = new Queue<Job>();
    private volatile bool ProcessWorking;
    private SpinWait spinWait;
    private ThreadUnit[] threads;

    /// <summary>Creates the manager and spins up the requested number of worker threads.</summary>
    public MultiThreadManager(int countThreads)
    {
        spinWait = new SpinWait();
        ChangeNumberThreads(countThreads);
    }

    /// <summary>The number of threads that were repaired after exceeding the critical work time.</summary>
    public int FailedThreadsCount { get; private set; }

    /// <summary>The current number of worker threads.</summary>
    public int ThreadsCount { get; private set; }

    /// <summary>Resizes the worker thread pool, aborting and recreating threads as needed.</summary>
    public void ChangeNumberThreads(int countThreads)
    {
        lock (locker)
        {
            if (countThreads == ThreadsCount)
                return;

            ThreadsCount = countThreads;

            if (threads != null)
            {
                foreach (var thread in threads)
                {
                    thread?.Abort();
                }

                while (!FreeThreads.IsEmpty)
                {
                    FreeThreads.TryDequeue(out _);
                }
            }

            if (countThreads > 0)
            {
                threads = new ThreadUnit[ThreadsCount];

                for (var i = 0; i < ThreadsCount; i++)
                {
                    threads[i] = new ThreadUnit($"Thread #{i}", i);
                    FreeThreads.Enqueue(threads[i]);
                }
            }
            else
                threads = null;
        }
    }

    /// <summary>Schedules a job, assigning it to a free thread immediately or queuing it for the next <see cref="Process"/>.</summary>
    public Job AddJob(Job job)
    {
        job.IsStarted = true;
        var jobAbsorbed = false;

        if (!FreeThreads.IsEmpty)
        {
            FreeThreads.TryDequeue(out var threadUnit);

            if (threadUnit != null)
            {
                jobAbsorbed = threadUnit.AddJob(job);

                if (threadUnit.Free)
                    FreeThreads.Enqueue(threadUnit);
            }
        }

        if (!jobAbsorbed)
            Jobs.Enqueue(job);

        return job;
    }

    /// <summary>Creates and schedules a named job wrapping the given action.</summary>
    public Job AddJob(Action action, string name)
    {
        var newJob = new Job(name, action);

        return AddJob(newJob);
    }

    /// <summary>Drains the queued jobs across the worker threads, blocking until they complete and repairing stuck threads.</summary>
    public void Process(object o)
    {
        if (threads == null)
            return;

        if (Interlocked.CompareExchange(ref _lock, 1, 0) == 1)
            return;

        if (ProcessWorking)
            DebugWindow.LogMsg($"WTF {_objectInitWork.GetType()}");

        _objectInitWork = o;
        ProcessWorking = true;
        spinWait.Reset();

        while (Jobs.TryDequeue(out var j))
        {
            processJobs.Enqueue(j);
        }

        if (ThreadsCount > 1)
        {
            while (processJobs.Count > 0)
            {
                if (!FreeThreads.IsEmpty)
                {
                    FreeThreads.TryDequeue(out var freeThread);
                    var job = processJobs.Dequeue();

                    if (!freeThread.AddJob(job))
                        processJobs.Enqueue(job);
                    else
                    {
                        if (freeThread.Free)
                            FreeThreads.Enqueue(freeThread);
                    }
                }
                else
                {
                    spinWait.SpinOnce();
                    var allThreadsBusy = true;

                    for (var i = 0; i < threads.Length; i++)
                    {
                        var th = threads[i];

                        if (th.Free)
                        {
                            allThreadsBusy = false;
                            FreeThreads.Enqueue(th);
                        }
                    }

                    if (allThreadsBusy)
                    {
                        for (var i = 0; i < threads.Length; i++)
                        {
                            var th = threads[i];
                            var thWorkingTime = th.WorkingTime;

                            if (thWorkingTime > CriticalWorkTimeMs)
                            {
                                DebugWindow.LogMsg(
                                    $"Repair thread #{th.Number} with Job1: {th.Job.Name} (C: {th.Job.IsCompleted} F: {th.Job.IsFailed}) && Job2:{th.SecondJob.Name} (C: {th.SecondJob.IsCompleted} F: {th.SecondJob.IsFailed}) Time: {thWorkingTime} > {thWorkingTime >= CriticalWorkTimeMs}",
                                    5);

                                th.Abort();
                                BrokenThreads.Add(th);
                                var newThread = new ThreadUnit($"Repair critical time {th.Number}", th.Number);
                                threads[th.Number] = newThread;
                                FreeThreads.Enqueue(newThread);
                                Thread.Sleep(5);
                                FailedThreadsCount++;
                                break;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            var threadUnit = threads[0];

            while (processJobs.Count > 0)
            {
                if (threadUnit.Free)
                {
                    var job = processJobs.Dequeue();
                    threadUnit.AddJob(job);
                }
                else
                {
                    spinWait.SpinOnce();
                    var threadUnitWorkingTime = threadUnit.WorkingTime;

                    if (threadUnitWorkingTime > CriticalWorkTimeMs)
                    {
                        DebugWindow.LogMsg(
                            $"Repair thread #{threadUnit.Number} withreadUnit Job1: {threadUnit.Job.Name} (C: {threadUnit.Job.IsCompleted} F: {threadUnit.Job.IsFailed}) && Job2:{threadUnit.SecondJob.Name} (C: {threadUnit.SecondJob.IsCompleted} F: {threadUnit.SecondJob.IsFailed}) Time: {threadUnitWorkingTime} > {threadUnitWorkingTime >= CriticalWorkTimeMs}",
                            5);

                        threadUnit.Abort();
                        BrokenThreads.Add(threadUnit);
                        threadUnit = new ThreadUnit($"Repair critical time {threadUnit.Number}", threadUnit.Number);
                        Thread.Sleep(5);
                        FailedThreadsCount++;
                    }
                }
            }
        }

        if (BrokenThreads.Count > 0)
        {
            var criticalWorkTimeMs = CriticalWorkTimeMs * 2;

            for (var index = 0; index < BrokenThreads.Count; index++)
            {
                var brokenThread = BrokenThreads[index];
                if (brokenThread == null) continue;

                if (brokenThread.WorkingTime > criticalWorkTimeMs)
                {
                    brokenThread.ForceAbort();
                    BrokenThreads[index] = null;
                }
            }

            if (BrokenThreads.AllF(x => x == null))
                BrokenThreads.Clear();
        }

        Interlocked.CompareExchange(ref _lock, 0, 1);
        ProcessWorking = false;
    }

    /// <summary>Aborts all worker threads and marks the manager as closed.</summary>
    public void Close()
    {
        foreach (var thread in threads)
        {
            thread.Abort();
        }

        Closed = true;
    }
}

/// <summary>
/// A long-lived worker thread that runs up to two queued <see cref="Job"/>s, waking on demand
/// via an <see cref="AutoResetEvent"/> and recording each job's outcome and elapsed time.
/// </summary>
public class ThreadUnit
{
    private readonly AutoResetEvent _event;
    private readonly Stopwatch sw;
    private readonly Thread thread;
    private bool _wait = true;
    private volatile bool abort;
    private bool running = true;

    /// <summary>Creates and starts a background worker thread with the given name and slot number.</summary>
    public ThreadUnit(string name, int number)
    {
        Number = number;

        Job = new Job("InitJob", null)
        {
            IsCompleted = true
        };

        SecondJob = new Job("InitJob", null)
        {
            IsCompleted = true
        };

        _event = new AutoResetEvent(false);

        thread = new Thread(DoWork);
        thread.Name = name;
        thread.IsBackground = true;
        thread.Start();
        sw = Stopwatch.StartNew();
    }

    /// <summary>Total number of jobs assigned across all thread units.</summary>
    public static int CountJobs { get; set; }

    /// <summary>Total number of times any thread unit has parked waiting for work.</summary>
    public static int CountWait { get; set; }

    /// <summary>This thread's slot number in the pool.</summary>
    public int Number { get; }

    /// <summary>The primary job slot.</summary>
    public Job Job { get; private set; }

    /// <summary>The secondary job slot.</summary>
    public Job SecondJob { get; private set; }

    /// <summary>True when at least one job slot is free.</summary>
    public bool Free => Job.IsCompleted || SecondJob.IsCompleted;

    /// <summary>Milliseconds elapsed since the current work started.</summary>
    public long WorkingTime => sw.ElapsedMilliseconds;

    private void DoWork()
    {
        while (running)
        {
            if (Job.IsCompleted && SecondJob.IsCompleted)
            {
                _event.WaitOne();
                CountWait++;
                _wait = true;
            }

            if (!Job.IsCompleted)
            {
                try
                {
                    sw.Restart();
                    Job.Work?.Invoke();
                }
                catch (Exception e)
                {
                    DebugWindow.LogError(e.ToString());
                    Job.IsFailed = true;
                }
                finally
                {
                    Job.ElapsedMs = sw.Elapsed.TotalMilliseconds;
                    Job.IsCompleted = true;
                    sw.Restart();
                }
            }

            if (!SecondJob.IsCompleted)
            {
                try
                {
                    sw.Restart();
                    SecondJob.Work?.Invoke();
                }
                catch (Exception e)
                {
                    DebugWindow.LogError(e.ToString());
                    SecondJob.IsFailed = true;
                }
                finally
                {
                    SecondJob.ElapsedMs = sw.Elapsed.TotalMilliseconds;
                    SecondJob.IsCompleted = true;
                    sw.Restart();
                }
            }
        }
    }

    /// <summary>Assigns the job to a free slot and wakes the thread. Returns whether a slot was available.</summary>
    public bool AddJob(Job job)
    {
        job.WorkingOnThread = this;
        var jobSetted = false;

        if (Job.IsCompleted)
        {
            Job = job;
            jobSetted = true;
            CountJobs++;
        }
        else if (SecondJob.IsCompleted)
        {
            SecondJob = job;
            jobSetted = true;
            CountJobs++;
        }

        if (_wait && jobSetted)
        {
            _wait = false;
            _event.Set();
        }

        return jobSetted;
    }

    /// <summary>Requests a cooperative stop: marks jobs complete/failed and ends the run loop.</summary>
    public void Abort()
    {
        Job.IsCompleted = true;
        SecondJob.IsCompleted = true;
        Job.IsFailed = true;
        Job.IsFailed = true;
        abort = true;

        if (_wait)
            _event.Set();

        running = false;
    }

    /// <summary>Forcefully aborts the underlying thread.</summary>
    public void ForceAbort()
    {
        abort = true;
        thread.Abort();
    }
}

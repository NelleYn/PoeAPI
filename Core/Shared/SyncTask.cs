using System;
using System.Collections.Generic;
using System.Linq;

namespace ExileCore.Shared;
public static class SyncTask
{
    public static SyncTask<T> FromResult<T>(T result)
    {
        SyncTask<T> syncTask = new SyncTask<T>(isSyncContinuation: true);
        syncTask.Awaiter.ResultTask.SetResult(result);
        return syncTask;
    }

    public static SyncTask<SyncTask<T>> WhenAny<T>(params SyncTask<T>[] tasks)
    {
        SyncTask<SyncTask<T>> aggregateTask = new SyncTask<SyncTask<T>>(isSyncContinuation: false);
        SyncTask<T> syncTask = tasks.FirstOrDefault((SyncTask<T> x) => x.Awaiter.IsCompleted);
        if (syncTask != null)
        {
            aggregateTask.GetAwaiter().ResultTask.SetResult(syncTask);
            return aggregateTask;
        }

        List<IDisposable> disposeList = new List<IDisposable>();
        foreach (SyncTask<T> syncTask2 in tasks)
        {
            disposeList.Add(syncTask2.Awaiter.RedirectExecutionQueue(aggregateTask.Awaiter));
        }

        foreach (SyncTask<T> childTask in tasks)
        {
            childTask.Awaiter.OnCompleted(delegate
            {
                if (aggregateTask.GetAwaiter().ResultTask.TrySetResult(childTask))
                {
                    foreach (IDisposable item in disposeList)
                    {
                        item.Dispose();
                    }
                }
            });
        }

        return aggregateTask;
    }
}
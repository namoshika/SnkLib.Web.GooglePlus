using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Threading
{
    public class AsyncLocker
    {
        Task _task;
        readonly System.Threading.SemaphoreSlim _syncer = new System.Threading.SemaphoreSlim(1, 1);

        [System.Diagnostics.DebuggerStepThrough]
        public async Task LockAsync(bool isForced, Func<bool> flagChecker, Func<Task> proc, Action cacheProc)
        {
            if (isForced == false && flagChecker() == false)
            {
                if (cacheProc != null)
                    cacheProc();
                return;
            }
            try
            {
                await _syncer.WaitAsync();
                if(_task != null)
                    await _task;

                if (_task != null && isForced == false && flagChecker() == false)
                {
                    if (cacheProc != null)
                        cacheProc();
                }
                else
                    await (_task = Task.Run(proc));
            }
            finally { _syncer.Release(); }
        }
    }
}

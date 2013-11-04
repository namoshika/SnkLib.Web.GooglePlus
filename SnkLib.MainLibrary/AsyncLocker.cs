using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Threading
{
    public class AsyncLocker
    {
        public AsyncLocker()
        {
            m_readerReleaser = Task.FromResult(new Releaser(this, false));
            m_writerReleaser = Task.FromResult(new Releaser(this, true));
            _latestUpdateDate = DateTime.MinValue;
        }
        TaskCompletionSource<Releaser> m_waitingReader = new TaskCompletionSource<Releaser>();
        readonly Queue<TaskCompletionSource<Releaser>> m_waitingWriters =　new Queue<TaskCompletionSource<Releaser>>();
        readonly Task<Releaser> m_readerReleaser;
        readonly Task<Releaser> m_writerReleaser;
        int m_readersWaiting;
        int m_status;
        DateTime _latestUpdateDate;

        [System.Diagnostics.DebuggerStepThrough]
        public async Task LockAsync(bool isForced, Func<bool> flagChecker, TimeSpan? intervalRestriction, Func<Task> proc, Action cacheProc)
        {
            intervalRestriction = intervalRestriction ?? TimeSpan.FromSeconds(3);
            using (var releaser = await ReaderLockAsync())
            {
                if (isForced == false && (flagChecker() == false || DateTime.UtcNow - _latestUpdateDate < intervalRestriction))
                {
                    if (cacheProc != null)
                        cacheProc();
                    return;
                }
                using (await releaser.Upgrade())
                {
                    if (isForced == false && (flagChecker() == false || DateTime.UtcNow - _latestUpdateDate < intervalRestriction))
                    {
                        if (cacheProc != null)
                            cacheProc();
                        return;
                    }
                    _latestUpdateDate = DateTime.UtcNow;
                    await proc();
                }
            }
        }
        Task<Releaser> ReaderLockAsync()
        {
            lock (m_waitingWriters)
            {
                if (m_status >= 0 && m_waitingWriters.Count == 0)
                {
                    ++m_status;
                    return m_readerReleaser;
                }
                else
                {
                    ++m_readersWaiting;
                    return m_waitingReader.Task.ContinueWith(t => t.Result);
                }
            }
        }
        Task<Releaser> WriterLockAsync()
        {
            lock (m_waitingWriters)
            {
                if (m_status == 0)
                {
                    m_status = -1;
                    return m_writerReleaser;
                }
                else
                {
                    var waiter = new TaskCompletionSource<Releaser>();
                    m_waitingWriters.Enqueue(waiter);
                    return waiter.Task;
                }
            }
        }
        void ReaderRelease()
        {
            TaskCompletionSource<Releaser> toWake = null;

            lock (m_waitingWriters)
            {
                --m_status;
                if (m_status == 0 && m_waitingWriters.Count > 0)
                {
                    m_status = -1;
                    toWake = m_waitingWriters.Dequeue();
                }
            }

            if (toWake != null)
                toWake.SetResult(new Releaser(this, true));
        }
        void WriterRelease()
        {
            TaskCompletionSource<Releaser> toWake = null;
            bool toWakeIsWriter = false;

            lock (m_waitingWriters)
            {
                if (m_waitingWriters.Count > 0)
                {
                    toWake = m_waitingWriters.Dequeue();
                    toWakeIsWriter = true;
                }
                else if (m_readersWaiting > 0)
                {
                    toWake = m_waitingReader;
                    m_status = m_readersWaiting;
                    m_readersWaiting = 0;
                    m_waitingReader = new TaskCompletionSource<Releaser>();
                }
                else m_status = 0;
            }

            if (toWake != null)
                toWake.SetResult(new Releaser(this, toWakeIsWriter));
        }

        struct Releaser : IDisposable
        {
            internal Releaser(AsyncLocker toRelease, bool writer)
            {
                m_toRelease = toRelease;
                m_writer = writer;
                _dispose = () =>
                    {
                        if (toRelease != null)
                        {
                            if (writer)
                                toRelease.WriterRelease();
                            else
                                toRelease.ReaderRelease();
                        }
                    };
            }
            readonly AsyncLocker m_toRelease;
            readonly bool m_writer;
            Action _dispose;

            public Task<IDisposable> Upgrade()
            {
                System.Diagnostics.Debug.Assert(m_writer == false);

                var locker = m_toRelease;
                var releaser = this;

                locker.ReaderRelease();
                return locker.WriterLockAsync().ContinueWith(
                    tsk => (IDisposable)new Disposer(
                        async () =>
                        {
                            tsk.Result.Dispose();
                            var comeBackReleser = await locker.ReaderLockAsync();
                            releaser._dispose = comeBackReleser.Dispose;
                        }));
            }
            public void Dispose()
            { _dispose(); }
        }
        class Disposer : IDisposable
        {
            public Disposer(Action action) { _action = action; }
            public void Dispose() { _action(); }
            Action _action;
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="ICWorkItem.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
namespace CHystrix.Threading
{
    internal interface ICWorkItem
    {
        void Do();

        bool CanDo(int timeout);

        bool Wait(int waitMilliseconds);

        Exception Exception { get; }

        bool IsCompleted { get; }

        bool IsCanceled { get; }

        bool IsFaulted { get; }

        CTaskStatus Status { get; }

        int ExeuteMilliseconds { get; }

        int RealExecuteMilliseconds { get; }

        bool MarkTimeout();

    }

}

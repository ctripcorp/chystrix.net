using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix.Threading
{

    internal interface ICThreadPool
    {

        CWorkItem<T> QueueWorkItem<T>(Func<T> act,EventHandler<StatusChangeEventArgs> onStatusChange=null);

        int MaxConcurrentCount { set; get; }

        int WorkItemTimeoutMiliseconds { set; get; }
    }
}

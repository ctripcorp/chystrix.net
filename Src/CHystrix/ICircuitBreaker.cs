using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    internal interface ICircuitBreaker
    {
        bool AllowRequest();

        bool IsOpen();

        /// <summary>
        /// Invoked on successful executions as part of feedback mechanism when in a half-open state.
        /// </summary>
        void MarkSuccess();
    }
}
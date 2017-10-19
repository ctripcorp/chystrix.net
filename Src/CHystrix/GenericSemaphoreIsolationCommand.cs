using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    internal class GenericSemaphoreIsolationCommand<T> : SemaphoreIsolationCommand<T>, IFallback<T>
    {
        private readonly Func<T> _Execute;
        private readonly Func<T> _GetFallback;

        private readonly bool _HasFallback;

        public GenericSemaphoreIsolationCommand(string commandKey, string groupKey, string domain, Func<T> execute, Func<T> getFallback, Action<ICommandConfigSet> configCommand)
            : this(null, commandKey, groupKey, domain, execute, getFallback, configCommand)
        {
        }

        public GenericSemaphoreIsolationCommand(string instanceKey,
            string commandKey, string groupKey, string domain, Func<T> execute, Func<T> getFallback, Action<ICommandConfigSet> configCommand)
            : base(instanceKey, commandKey, groupKey, domain, configCommand, getFallback != null)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
                throw new ArgumentNullException("CommandKey cannot be null.");

            if (execute == null)
                throw new ArgumentNullException("execute function cannot be null.");

            _Execute = execute;
            _GetFallback = getFallback;
            _HasFallback = _GetFallback != null;
        }

        internal override bool HasFallback
        {
            get { return _HasFallback; }
        }

        protected override T Execute()
        {
            return _Execute();
        }

        public T GetFallback()
        {
            if (HasFallback)
                return _GetFallback();

            throw new NotImplementedException();
        }
    }
}

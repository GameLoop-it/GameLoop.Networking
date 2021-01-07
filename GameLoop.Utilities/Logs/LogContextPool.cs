/*
The MIT License (MIT)

Copyright (c) 2020 Emanuele Manzione

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.Collections.Generic;

namespace GameLoop.Utilities.Logs
{
    internal class LogContextPool
    {
        private readonly Queue<LogContext> _infoContexts;
        private readonly Queue<LogContext> _warningContexts;
        private readonly Queue<LogContext> _errorContexts;

        private readonly Func<LogContext> _infoFactory;
        private readonly Func<LogContext> _warningFactory;
        private readonly Func<LogContext> _errorFactory;

        public LogContextPool(Func<LogContext> infoFactory, Func<LogContext> warningFactory, Func<LogContext> errorFactory)
        {
            _infoContexts    = new Queue<LogContext>();
            _warningContexts = new Queue<LogContext>();
            _errorContexts   = new Queue<LogContext>();

            _infoFactory    = infoFactory;
            _warningFactory = warningFactory;
            _errorFactory   = errorFactory;
        }

        public LogContext GetInfoContext()
        {
            lock (_infoContexts)
            {
                if (_infoContexts.Count > 0)
                    return _infoContexts.Dequeue();
            }

            return _infoFactory.Invoke();
        }

        public LogContext GetWarningContext()
        {
            lock (_warningContexts)
            {
                if (_warningContexts.Count > 0)
                    return _warningContexts.Dequeue();
            }

            return _warningFactory.Invoke();
        }

        public LogContext GetErrorContext()
        {
            lock (_errorContexts)
            {
                if (_errorContexts.Count > 0)
                    return _errorContexts.Dequeue();
            }

            return _errorFactory.Invoke();
        }

        public void ReturnInfoContext(LogContext context)
        {
            lock (_infoContexts)
                _infoContexts.Enqueue(context);
        }

        public void ReturnWarningContext(LogContext context)
        {
            lock (_warningContexts)
                _warningContexts.Enqueue(context);
        }

        public void ReturnErrorContext(LogContext context)
        {
            lock (_errorContexts)
                _errorContexts.Enqueue(context);
        }
    }
}
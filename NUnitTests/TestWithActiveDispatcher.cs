﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace NUnitTests
{
    /// <summary>
    /// Abstract class which provides functionality used to create
    /// a unit test for a class which requires an active Dispatcher
    /// on an STA thread.
    /// </summary>
    public abstract class TestWithActiveDispatcher
    {
        #region Data

        const string WORKER_THREAD_NAME = "TestRunner";

        #endregion // Data

        #region BeginExecuteTest

        /// <summary>
        /// Calling this method causes the ExecuteTestAsync override to be
        /// invoked in the child class.  Invoke this method to initiate the
        /// asynchronous testing.
        /// </summary>
        protected void BeginExecuteTest(Action action)
        {
            Debug.Assert(this.IsRunningOnWorkerThread == false);

            // Run the test code on an STA worker thread
            // and then wait for that thread to die before
            // this method continues executing.  We use an
            // STA thread because many WPF classes require it.
            // We must do this work on a worker thread because
            // once a thread's Dispatcher is shut down it cannot
            // be run again.
            Thread thread = new Thread(this.BeginExecuteTestAsync);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Name = WORKER_THREAD_NAME;
            thread.Start(action);
            thread.Join();
        }

        void BeginExecuteTestAsync(object action)
        {
            Debug.Assert(this.IsRunningOnWorkerThread);

            // The call to ExecuteTestAsync must be delayed so 
            // that the worker thread's Dispatcher can be started
            // before the method executes.  This is needed because
            // the Dispatcher.Run method does not return until the
            // Dispatcher has been shut down.
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)action);

            // Start the worker thread's message pump so that
            // queued messages are processed.
            Dispatcher.Run();
        }

        #endregion // BeginExecuteTest

        #region IsRunningOnWorkerThread

        /// <summary>
        /// Returns true if this property was accessed on the worker thread
        /// which is spawned to execute the test logic.
        /// </summary>
        protected bool IsRunningOnWorkerThread
        {
            get { return Thread.CurrentThread.Name == WORKER_THREAD_NAME; }
        }

        #endregion // IsRunningOnWorkerThread

        #region EndExecuteTest

        /// <summary>
        /// Stops the worker thread's message pump.  Derived classes
        /// should call this method at the end of their ExecuteTestAsync
        /// override to shut down the worker thread's Dispatcher.
        /// Note: this method must be invoked from the worker thread.
        /// </summary>
        protected void EndExecuteTest()
        {
            Debug.Assert(this.IsRunningOnWorkerThread);

            Dispatcher disp = Dispatcher.CurrentDispatcher;
            Debug.Assert(disp.HasShutdownStarted == false);

            // Kill the worker thread's Dispatcher so that the
            // message pump is shut down and the thread can die.
            disp.BeginInvokeShutdown(DispatcherPriority.Normal);
        }

        #endregion // EndExecuteTest

        #region WaitWithoutBlockingDispatcher

        /// <summary>
        /// Pauses the worker thread for the specified duration, but allows
        /// the Dispatcher to continue processing messages in its message queue.
        /// Note: this method must be invoked from the worker thread.
        /// </summary>
        /// <param name="waitTime">The amount of time to pause.</param>
        protected void WaitWithoutBlockingDispatcher(TimeSpan waitTime)
        {
            Debug.Assert(this.IsRunningOnWorkerThread);

            // Save the time at which the wait began.
            DateTime startTime = DateTime.Now;

            bool wait = true;
            while (wait)
            {
                // Call DoEvents so that all of the messages in 
                // the worker thread's Dispatcher message queue
                // are processed.
                DispatcherHelper.DoEvents();

                // Check if the wait is over.
                TimeSpan elapsed = DateTime.Now - startTime;
                wait = elapsed < waitTime;
            }
        }

        #endregion // WaitWithoutBlockingDispatcher
    }
}
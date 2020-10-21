//-----------------------------------------------------------------------------
// FILE:	    Main.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2019 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

//-----------------------------------------------------------------------------
// This sample demonstrates synchronous signals.  This is an experimental feature.
// Normal Cadence signals are fire-and-forget.  This means that when a caller sends
// a signal that the send method will return once the signal has been submitted to
// Cadence and the caller will not know when the signal will actually be received
// annd processed by the workflow.  As a consequence, normal signals cannot return
// a result.
//
// Synchronous signals are different.  For these, the signal send methods won't
// return until after the signal has been received and processed by the workflow
// and synchronous signals may return a result.
//
// This sample includes two different ways you can implement a simple order 
// processing scenario where it's possible to cancel an order anytime before
// the order has shipped.  We'll provide a synchronous cancellation signal and
// the signal will return TRUE if the order could be and was cancelled or FALSE
// when it's too late to cancel the order.
//
// Requirements:
// -------------
// You'll need to have Docker installed as described in the repo README.md 
// have Cadence running locally via:
//
//      docker run --detach --name cadence-dev -p 7933-7939:7933-7939 -p 8088:8088 nkubeio/cadence-dev
//
// You can view that Cadence portal at:
//
//      http://localhost:8088/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

using Neon.Cadence;

namespace hello_workflow
{
    //-------------------------------------------------------------------------
    // This implements a contrived order processing workflow with a uses
    // polling and workflow fields to implement a cancellation signal.

    [WorkflowInterface(TaskList = "hello-tasks")]
    public interface IOrderWorkflow1 : IWorkflow
    {
        /// <summary>
        /// Processes the order.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the order was processed successfully, <c>false</c>
        /// if it was cancelled.
        /// </returns>
        [WorkflowMethod]
        Task<bool> ProcessAsync();

        /// <summary>
        /// Attempts to cancel the order.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the order was cancelled, <c>false</c> when it's 
        /// too late to cancel the order.
        /// </returns>
        [SignalMethod("cancel", Synchronous = true)]
        Task<bool> CancelAsync();
    }

    [Workflow(AutoRegister = true)]
    public class OrderWorkflow1 : WorkflowBase, IOrderWorkflow1
    {
        private enum OrderStatus
        {
            Cancelled = -1,
            Pending   = 0,
            Picking   = 1,
            Packing   = 2,
            Shipped   = 3
        }

        private OrderStatus status;

        public async Task<bool> ProcessAsync()
        {
            status = OrderStatus.Pending;

            //---------------------------------------------
            // STEP-1: Collect the order items from inventory.

            status = OrderStatus.Picking;

            // This is where you'd execute one or more activities telling
            // your inventory team (or robots) what they need to gather
            // for this order.  In real life, this would also probably
            // wait for a signal from an external system indicating that
            // the items have been collected and that the workflow can
            // proceed to the next step.
            //
            // We're going to leave this to your imagination to keep the
            // example simple.

            await Workflow.SleepAsync(TimeSpan.FromSeconds(10));

            // Abort order processing if an cancellation signal has been
            // received.

            if (status == OrderStatus.Cancelled)
            {
                return false;
            }

            //---------------------------------------------
            // STEP-2: Pack the order.

            status = OrderStatus.Packing;

            // This is where you'd execute one or more activities telling
            // your packers what to do as well as printing labels and ordering
            // a pickup from your shipper.

            await Workflow.SleepAsync(TimeSpan.FromSeconds(10));

            // Abort order processing if an cancellation signal has been
            // received.

            if (status == OrderStatus.Cancelled)
            {
                return false;
            }

            //---------------------------------------------
            // STEP-3: The order is considered to have shipped at this point
            //         and cancellation is no longer an option.

            status = OrderStatus.Shipped;

            // This is where the delivery would be tracked and potential
            // return logic would live.

            await Workflow.SleepAsync(TimeSpan.FromSeconds(5));

            return status != OrderStatus.Cancelled;
        }

        public async Task<bool> CancelAsync()
        {
            // Don't allow cancellation after the order has shipped.

            if (status < OrderStatus.Shipped)
            {
                status = OrderStatus.Cancelled;

                return await Task.FromResult(true);
            }
            else
            {
                return await Task.FromResult(false);
            }
        }
    }

    //-------------------------------------------------------------------------
    // This implements a contrived order processing workflow that uses a
    // workflow queue to marshal a cancellation signal into the workflow
    // logic.

    [WorkflowInterface(TaskList = "hello-tasks")]
    public interface IOrderWorkflow2 : IWorkflow
    {
        /// <summary>
        /// Processes the order.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the order was processed successfully, <c>false</c>
        /// if it was cancelled.
        /// </returns>
        [WorkflowMethod]
        Task<bool> ProcessAsync();

        /// <summary>
        /// Attempts to cancel the order.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the order was cancelled, <c>false</c> when it's 
        /// too late to cancel the order.
        /// </returns>
        [SignalMethod("cancel", Synchronous = true)]
        Task<bool> CancelAsync();
    }

    [Workflow(AutoRegister = true)]
    public class OrderWorkflow2 : WorkflowBase, IOrderWorkflow2
    {
        private enum OrderStatus
        {
            Cancelled = -1,
            Pending   = 0,
            Picking   = 1,
            Packing   = 2,
            Shipped   = 3
        }

        private OrderStatus                         status;
        private WorkflowQueue<SignalRequest<bool>>  cancelQueue;

        public async Task<bool> ProcessAsync()
        {
            status      = OrderStatus.Pending;
            cancelQueue = await Workflow.NewQueueAsync<SignalRequest<bool>>();

            //---------------------------------------------
            // STEP-1: Collect the order items from inventory.

            status = OrderStatus.Picking;

            // This is where you'd execute one or more activities telling
            // your inventory team (or robots) what they need to gather
            // for this order.  In real life, this would also probably
            // wait for a signal from an external system indicating that
            // the items have been collected and that the workflow can
            // proceed to the next step.
            //
            // We're going to leave this to your imagination to keep the
            // example simple.

            await Workflow.SleepAsync(TimeSpan.FromSeconds(5));

            // Abort order processing if an cancellation signal has been
            // received.

            try
            {
                var signalRequest = await cancelQueue.DequeueAsync(timeout: TimeSpan.FromSeconds(1));

                // This call indicates that the signal method that enqueued
                // the signal should return TRUE.

                await signalRequest.ReplyAsync(true);

                // This terminates the workflow.

                return false;
            }
            catch (CadenceTimeoutException)
            {
                // There was no signal pending.
            }

            //---------------------------------------------
            // STEP-2: Pack the order.

            status = OrderStatus.Packing;

            // This is where you'd execute one or more activities telling
            // your packers what to do as well as printing labels and ordering
            // a pickup from your shipper.

            await Workflow.SleepAsync(TimeSpan.FromSeconds(5));

            // Abort order processing if an cancellation signal has been
            // received.

            try
            {
                var signalRequest = await cancelQueue.DequeueAsync(timeout: TimeSpan.FromSeconds(1));

                // This call indicates that the signal method that enqueued
                // the signal should return TRUE.

                await signalRequest.ReplyAsync(true);

                // This terminates the workflow.

                return false;
            }
            catch (CadenceTimeoutException)
            {
                // There was no signal pending.
            }

            //---------------------------------------------
            // STEP-3: The order is considered to have shipped at this point
            //         and cancellation is no longer an option.

            status = OrderStatus.Shipped;

            // This is where the delivery would be tracked and potential
            // return logic would live.

            await Workflow.SleepAsync(TimeSpan.FromSeconds(5));

            return status != OrderStatus.Cancelled;
        }

        public async Task<bool> CancelAsync()
        {
            // We're going to enqueue a signal request to marshal it
            // into the workflow logic which will dequeue it.
            //
            // The [SignalRequest] constructor here is a bit magical.
            // The generic parameter specifies the result type that
            // will be returned by the workflow logic.  This is often
            // the same as the signal methods return type, but it
            // doesn't have to be the same.
            //
            // The magic part is that the request constructor automatically
            // initializes its [Args] dictionary with the names and values
            // of any arguments passed to this method.  In this example,
            // there are no parameters but if there were any, they would
            // have been added to the request so they'd be available for
            // the workflow logic.

            await cancelQueue.EnqueueAsync(new SignalRequest<bool>());

            // Throwing this exception indicates to the Cadence client
            // that the signal result will be sent as a reply from
            // the workflow code via the [SignalRequest] rather than 
            // via a result returned by this signal method.
            //
            // We understand that this is a bit odd, but this is an
            // experimental feature after all.  The Cadence team is
            // working on a new feature to handle these scenarios
            // cleanly.

            throw new WaitForSignalReplyException();
        }
    }

    //-------------------------------------------------------------------------
    // Program entry point

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var settings = new CadenceSettings("cadence://localhost:7933")
                {
                    DefaultDomain = "test-domain",
                    CreateDomain  = true
                };

                using (var client = await CadenceClient.ConnectAsync(settings))
                {
                    await client.RegisterAssemblyAsync(Assembly.GetExecutingAssembly());
                    await client.StartWorkerAsync(taskList: "hello-tasks");

                    //-------------------------------------
                    // Submit an order to: IOrderWorkflow1

                    var stub1      = client.NewWorkflowStub<IOrderWorkflow1>();
                    var orderTask1 = stub1.ProcessAsync();

                    // Attempt to cancel it via a synchronous signal.

                    var cancelled1 = await stub1.CancelAsync();

                    // Wait for order processing to complete.  The result will
                    // be FALSE if the order was cancelled.

                    var result1 = await orderTask1;

                    //-------------------------------------
                    // Submit an order to: IOrderWorkflow2

                    var stub2      = client.NewWorkflowStub<IOrderWorkflow2>();
                    var orderTask2 = stub2.ProcessAsync();

                    // Attempt to cancel it via a synchronous signal.

                    var cancelled2 = await stub2.CancelAsync();

                    // Wait for order processing to complete.  The result will
                    // be FALSE if the order was cancelled.

                    var result2 = await orderTask2;

                    //-------------------------------------

                    Console.WriteLine($"RESULT-1: {result1}");
                    Console.WriteLine($"RESULT-2: {result2}");
                }
            }
            catch (ConnectException)
            {
                Console.Error.WriteLine("Cannot connect to Cadence.  Be sure you've started a");
                Console.Error.WriteLine("local Cadence Docker container via:");
                Console.Error.WriteLine();
                Console.Error.WriteLine("docker run --detach --name cadence-dev -p 7933-7939:7933-7939 -p 8088:8088 nkubeio/cadence-dev");
            }
        }
    }
}

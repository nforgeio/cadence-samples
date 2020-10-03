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
// This sample demonstrates how to write a very simple workflow that waits for
// signal and then returns a result that includes a signal argument value.  This
// is a somewhat contrived example that demonstrates how a signal method can use
// a WorkflowQueue to marshal a received signal to workflow method logic.
//
// An alternative approach would be to have the signal method set a workflow
// instance field and then have the workflow logic implement a polling loop
// to wait for this field to be updated by the signal method.
// 
// Note that normal Cadence signals are fire-and-forget and cannot return a
// value to the caller.  The Neon.Cadence library does include experimental
// support for synchronos signals that don't return to the caller until the
// signal has been processed and these synchronous signals may return a result.
// See the hello-syncsignal project for an example.
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
    [WorkflowInterface(TaskList = "hello-tasks")]
    public interface IHelloWorkflow : IWorkflow
    {
        [WorkflowMethod]
        Task<string> HelloAsync();

        [SignalMethod("signal")]
        Task SignalNameAsync(string name);
    }

    [Workflow(AutoRegister = true)]
    public class HelloWorkflow : WorkflowBase, IHelloWorkflow
    {
        private WorkflowQueue<string>   signalQueue;

        public async Task<string> HelloAsync()
        {
            signalQueue = await Workflow.NewQueueAsync<string>();

            var name = await signalQueue.DequeueAsync();

            return $"Hello {name}!";
        }

        public async Task SignalNameAsync(string name)
        {
            await signalQueue.EnqueueAsync(name);
        }
    }

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

                    var stub   = client.NewWorkflowFutureStub<IHelloWorkflow>();
                    var future = await stub.StartAsync<string>();

                    await stub.SignalAsync("signal", "Sally");

                    var result = await future.GetAsync();

                    Console.WriteLine($"RESULT: {result}");
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

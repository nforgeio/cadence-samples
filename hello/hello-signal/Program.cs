﻿//-----------------------------------------------------------------------------
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
// signal and then returns a result that includes the signal value.
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

        [SignalMethod("signal-name")]
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

                await stub.SignalAsync("signal-name", "Sally");

                var result = await future.GetAsync();

                Console.WriteLine($"RESULT: {result}");
            }
        }
    }
}
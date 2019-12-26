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
// This sample demonstrates how to write a very simple workflow and activity,
// register then with Cadence.  Then we'll execute the workflow which calls
// the activity and then the workflow returns the activity result.
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
    [ActivityInterface(TaskList = "hello-tasks")]
    public interface IHelloActivity : IActivity
    {
        [ActivityMethod]
        Task<string> HelloAsync(string name);
    }

    [Activity(AutoRegister = true)]
    public class HelloActivity : ActivityBase, IHelloActivity
    {
        public async Task<string> HelloAsync(string name)
        {
            return await Task.FromResult($"Hello {name}!");
        }
    }

    [WorkflowInterface(TaskList = "hello-tasks")]
    public interface IHelloWorkflow : IWorkflow
    {
        [WorkflowMethod]
        Task<string> HelloAsync(string name);
    }

    [Workflow(AutoRegister = true)]
    public class HelloWorkflow : WorkflowBase, IHelloWorkflow
    {
        public async Task<string> HelloAsync(string name)
        {
            var stub = Workflow.NewActivityStub<IHelloActivity>();

            return await stub.HelloAsync(name);
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

                    var stub = client.NewWorkflowStub<IHelloWorkflow>();
                    var result = await stub.HelloAsync("Sally");

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

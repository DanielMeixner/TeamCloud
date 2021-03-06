/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Context;
using TeamCloud.Model.Data;
using TeamCloud.Orchestrator.Activities;

namespace TeamCloud.Orchestrator.Orchestrations
{
    public static class ProjectUserCreateOrchestration
    {
        [FunctionName(nameof(ProjectUserCreateOrchestration))]
        public static async Task<bool> RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            (OrchestratorContext orchestratorContext, ProjectUserCreateCommand command) = functionContext.GetInput<(OrchestratorContext, ProjectUserCreateCommand)>();

            var user = command.Payload;

            var project = await functionContext.CallActivityAsync<Project>(nameof(ProjectUserCreateActivity), (orchestratorContext.Project, user));

            //functionContext.WaitForExternalEvent()

            // TODO: call set users on all providers
            // var tasks = input.teamCloud.Configuration.Providers.Select(p =>
            //                 context.CallHttpAsync(HttpMethod.Post, p.Location, JsonConvert.SerializeObject(projectContext)));

            // await Task.WhenAll(tasks);

            return true;
        }
    }
}
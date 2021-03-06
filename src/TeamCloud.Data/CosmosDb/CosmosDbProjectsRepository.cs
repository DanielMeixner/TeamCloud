﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Azure.Cosmos;
using TeamCloud.Model;
using TeamCloud.Model.Data;

namespace TeamCloud.Data.CosmosDb
{

    public class CosmosDbProjectsRepository : IProjectsRepository
    {
        private readonly CosmosDbContainerFactory containerFactory;

        public CosmosDbProjectsRepository(ICosmosDbOptions cosmosOptions)
        {
            containerFactory = CosmosDbContainerFactory.Get(cosmosOptions);
        }

        private Task<Container> GetContainerAsync()
            => containerFactory.GetContainerAsync<Project>();

        public async Task<Project> AddAsync(Project project)
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var response = await container
                .CreateItemAsync(project)
                .ConfigureAwait(false);

            return response.Value;
        }

        public async Task<Project> GetAsync(Guid projectId)
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            try
            {
                var response = await container
                    .ReadItemAsync<Project>(projectId.ToString(), new PartitionKey(projectId.ToString()))
                    .ConfigureAwait(false);

                return response.Value;
            }
            catch (CosmosException cosmosEx)
            {
                if (cosmosEx.Status == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw;
            }
        }

        public async Task<Project> SetAsync(Project project)
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var response = await container
                .UpsertItemAsync<Project>(project, new PartitionKey(project.Id.ToString()))
                .ConfigureAwait(false);

            return response.Value;
        }

        public async IAsyncEnumerable<Project> ListAsync(Guid? userId = null)
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var query = new QueryDefinition($"SELECT * FROM c");
            var queryIterator = container.GetItemQueryIterator<Project>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(Constants.CosmosDb.TeamCloudInstanceId) });

            await foreach (var queryResult in queryIterator)
            {
                yield return queryResult;
            }
        }

        public async Task<Project> RemoveAsync(Project project)
        {
            var container = await GetContainerAsync()
                .ConfigureAwait(false);

            var response = await container
                .DeleteItemAsync<Project>(project.Id.ToString(), new PartitionKey(project.Id.ToString()))
                .ConfigureAwait(false);

            return response.Value;
        }
    }
}

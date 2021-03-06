﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Flurl;
using Flurl.Http;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamCloud.Azure
{
    public interface IAzureDirectoryService
    {
        Task<Guid?> GetUserIdAsync(string identifier);

        Task<Guid?> GetGroupIdAsync(string identifier);
    }

    public class AzureDirectoryService : IAzureDirectoryService
    {
        private readonly IAzureSessionFactory azureSessionFactory;

        public AzureDirectoryService(IAzureSessionFactory azureSessionFactory)
        {
            this.azureSessionFactory = azureSessionFactory ?? throw new ArgumentNullException(nameof(azureSessionFactory));
        }

        private async Task<string> GetDefaultDomainAsync()
        {
            var token = await azureSessionFactory
                .AcquireTokenAsync(AzureAuthorities.AzureGraph)
                .ConfigureAwait(false);

            var json = await AzureAuthorities.AzureGraph
                .AppendPathSegment($"{azureSessionFactory.TenantId}/tenantDetails")
                .SetQueryParam("api-version", "1.6")
                .WithOAuthBearerToken(token)
                .GetJObjectAsync();

            return json.SelectToken("$.value[0].verifiedDomains[?(@.default == true)].name")?.ToString();
        }

        private async Task<IEnumerable<string>> GetVerifiedDomainsAsync()
        {
            var token = await azureSessionFactory
                .AcquireTokenAsync(AzureAuthorities.AzureGraph)
                .ConfigureAwait(false);

            var json = await AzureAuthorities.AzureGraph
                .AppendPathSegment($"{azureSessionFactory.TenantId}/tenantDetails")
                .SetQueryParam("api-version", "1.6")
                .WithOAuthBearerToken(token)
                .GetJObjectAsync()
                .ConfigureAwait(false);

            return json.SelectTokens("$.value[0].verifiedDomains[*].name").Select(name => name.ToString());
        }

        public async Task<Guid?> GetUserIdAsync(string identifier)
        {
            if (identifier is null)
                throw new ArgumentNullException(nameof(identifier));

            var azureSession = azureSessionFactory.CreateSession();
            var azureUser = default(IActiveDirectoryUser);

            if (identifier.IsEMail())
            {
                var verifiedDomains = await GetVerifiedDomainsAsync()
                    .ConfigureAwait(false);

                if (!verifiedDomains.Any(domain => identifier.EndsWith($"@{domain}", StringComparison.OrdinalIgnoreCase)))
                {
                    var defaultDomain = await GetDefaultDomainAsync().ConfigureAwait(false);

                    identifier = $"{identifier.Replace("@", "_")}#EXT#@{defaultDomain}";
                }

                azureUser = await azureSession.ActiveDirectoryUsers
                    .GetByNameAsync(identifier)
                    .ConfigureAwait(false);
            }
            else if (identifier.IsGuid())
            {
                azureUser = await azureSession.ActiveDirectoryUsers
                    .GetByIdAsync(identifier)
                    .ConfigureAwait(false);
            }

            if (azureUser is null) return null;

            return Guid.Parse(azureUser.Inner.ObjectId);
        }

        public Task<Guid?> GetGroupIdAsync(string identifier)
        {
            throw new NotImplementedException();
        }
    }
}

﻿using Autofac.Multitenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Sandbox.Shared
{
    public class QueryStringTenantIdentificationStrategy : ITenantIdentificationStrategy
    {
        private readonly ILogger<QueryStringTenantIdentificationStrategy> _logger;

        public QueryStringTenantIdentificationStrategy(IHttpContextAccessor accessor, ILogger<QueryStringTenantIdentificationStrategy> logger)
        {
            this.Accessor = accessor;
            this._logger = logger;
        }

        public IHttpContextAccessor Accessor { get; private set; }

        public bool TryIdentifyTenant(out object tenantId)
        {
            var context = this.Accessor.HttpContext;
            if (context == null)
            {
                // No current HttpContext. This happens during app startup
                // and isn't really an error, but is something to be aware of.
                tenantId = null;
                return false;
            }

            // Caching the value both speeds up tenant identification for
            // later and ensures we only see one log message indicating
            // relative success or failure for tenant ID.
            if (context.Items.TryGetValue("_tenantId", out tenantId))
            {
                // We've already identified the tenant at some point
                // so just return the cached value (even if the cached value
                // indicates we couldn't identify the tenant for this context).
                return tenantId != null;
            }

            if (context.Request.Query.TryGetValue("tenant", out StringValues tenantValues))
            {
                tenantId = tenantValues[0];
                context.Items["_tenantId"] = tenantId;
                this._logger.LogInformation("Identified tenant: {tenant}", tenantId);
                return true;
            }

            this._logger.LogWarning("Unable to identify tenant from query string. Falling back to default.");
            tenantId = null;
            context.Items["_tenantId"] = null;
            return false;
        }
    }
}

﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GagspeakShared.Data;
using Microsoft.EntityFrameworkCore;
using GagspeakShared.Utils;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GagspeakShared.RequirementHandlers;

/// <summary> The handler for the UserRequirement. </summary>
public class UserRequirementHandler : AuthorizationHandler<UserRequirement, HubInvocationContext>
{
    private readonly GagspeakDbContext _dbContext;
    private readonly ILogger<UserRequirementHandler> _logger;
    private readonly IRedisDatabase _redis;

    public UserRequirementHandler(GagspeakDbContext dbContext, ILogger<UserRequirementHandler> logger, IRedisDatabase redisDb)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redisDb;
    }

    /// <summary> Handles the requirement for the user. </summary>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserRequirement requirement, HubInvocationContext resource)
    {
        // output the requirements to the logger
        //_logger.LogInformation("Requirements for access to function: {requirements}", requirement.Requirements);
        // first before anything we should see if the context user contains the claims for temporary access. If they do, we should set the user requirements to identify them as a temporary access connection.
        if ((requirement.Requirements & UserRequirements.TemporaryAccess) is UserRequirements.TemporaryAccess)
        {
            var hasLocalContentAccess = context.User.Claims.Any(c => string.Equals(c.Type, GagspeakClaimTypes.AccessType, StringComparison.Ordinal)
                                                 && string.Equals(c.Value, "LocalContent", StringComparison.Ordinal));
            
            // if the user does not have temporary access, fail the context
            if (!hasLocalContentAccess) context.Fail();
        }


        // if the requirement is Identified, check if the UID is in the Redis database
        if ((requirement.Requirements & UserRequirements.Identified) is UserRequirements.Identified)
        {
            //_logger.LogInformation("Validating Identified requirement");
            // Get the UID from the context claim
            var uid = context.User.Claims.SingleOrDefault(g => string.Equals(g.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))?.Value;
            // if the UID is null, fail the context
            //_logger.LogInformation("GagspeakHub:UID: {uid}", uid);

            if (uid is null) context.Fail();
            // fetch the ident(ity) from the Redis database
            
            //_logger.LogInformation("Fetching ident from Redis");
            
            // ar ident = await _redis.GetAsync<string>("GagspeakHub:UID:" + uid).ConfigureAwait(false);
            var ident = await _redis.GetAsync<string>("GagspeakHub:UID:" + uid).ConfigureAwait(false);
            if (ident == RedisValue.EmptyString)
                context.Fail();

        }

        // otherwise, succeed the context requirement.
        context.Succeed(requirement);
    }
}

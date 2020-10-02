﻿using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace SogigiMind.Authentication
{
    public static class EndUserAuthorizationPolicy
    {
        public const string PolicyName = "EndUser";

        public static void AddEndUserPolicy(this AuthorizationOptions options)
        {
            options.AddPolicy(PolicyName, policy => policy.RequireAssertion(ctx =>
                ctx.User?.Claims.Count(claim => claim.Type == SogigiMindClaimTypes.Acct) == 1));
        }
    }
}

using System;
using Microsoft.AspNetCore.Authentication;

namespace SogigiMind.TestInfrastructures
{
    internal class ConstantClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; }

        public ConstantClock(DateTimeOffset now)
        {
            this.UtcNow = now.ToUniversalTime();
        }
    }
}

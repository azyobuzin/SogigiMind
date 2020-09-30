using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SogigiMind.Infrastructures
{
    public static class OptionsBuilderExtensions
    {
        public static OptionsBuilder<TOptions> BindManually<TOptions, TConfiguration>(
            this OptionsBuilder<TOptions> optionsBuilder,
            TConfiguration config,
            Action<TOptions, TConfiguration> configureOptions)
            where TOptions : class
            where TConfiguration : IConfiguration
        {
            optionsBuilder.Services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(new ConfigurationChangeTokenSource<TOptions>(optionsBuilder.Name, config));
            return optionsBuilder.Configure(options => configureOptions(options, config));
        }
    }
}

﻿namespace SemDiff.Core.Configuration
{
    using System.Configuration;

    public sealed class AuthenticationSection : ConfigurationSection
    {
        [ConfigurationProperty("authentication", IsRequired = false, DefaultValue = null)]
        public AuthenticationElement Authentication
        {
            get
            {
                return (AuthenticationElement)this["authentication"];
            }
            set
            {
                this["authentication"] = value;
            }
        }
    }
}
namespace SemDiff.Test.Configuration
{
    using System.Configuration;

    public sealed class AuthenticationUnitTestsSection : ConfigurationSection
    {
        [ConfigurationProperty("authentication", IsRequired = false, DefaultValue = null)]
        public AuthenticationUnitTestsElement Authentication
        {
            get
            {
                return (AuthenticationUnitTestsElement)this["authentication"];
            }
            set
            {
                this["authentication"] = value;
            }
        }
    }
}
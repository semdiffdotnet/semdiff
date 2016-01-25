namespace SemDiff.Core.Configuration
{
    using System.Configuration;

    public sealed class AuthenticationElement : ConfigurationElement
    {
        [ConfigurationProperty("token", IsRequired = false)]
        public string Token
        {
            get
            {
                return (string)this["token"];
            }
            set
            {
                this["token"] = value;
            }
        }
    }
}

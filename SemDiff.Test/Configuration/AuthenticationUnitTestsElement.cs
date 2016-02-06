namespace SemDiff.Test.Configuration
{
    using System.Configuration;

    public sealed class AuthenticationUnitTestsElement : ConfigurationElement
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

        [ConfigurationProperty("username", IsRequired = false)]
        public string Username
        {
            get
            {
                return (string)this["username"];
            }
            set
            {
                this["username"] = value;
            }
        }
    }
}
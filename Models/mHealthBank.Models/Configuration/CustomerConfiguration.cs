using System.Collections.Generic;

namespace mHealthBank.Models.Configuration
{
    public class CustomerConfiguration
    {
        public static readonly string _ConfigurationName = "CustomerConfiguration";

        public string BasePath { get; set; }
        public IEnumerable<string> AllowedExtension { get; set; }
    }
}

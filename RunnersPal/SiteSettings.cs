using Massive;

namespace RunnersPal
{
    public class SiteSettings : DynamicModel
    {
        public SiteSettings() : base(MassiveDB.ConnectionStringName, "SiteSettings", "Id") { }
    }
}

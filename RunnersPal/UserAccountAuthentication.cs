using Massive;

namespace RunnersPal
{
    public class UserAccountAuthentication : DynamicModel
    {
        public UserAccountAuthentication() : base(MassiveDB.ConnectionStringName, "UserAccountAuthentication", "Id") { }
    }
}

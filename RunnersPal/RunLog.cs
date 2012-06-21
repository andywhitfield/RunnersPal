using Massive;

namespace RunnersPal
{
    public class RunLog : DynamicModel
    {
        public RunLog() : base(MassiveDB.ConnectionStringName, "RunLog", "Id") { }
    }

    public static class RunLogExtensions
    {
        public static dynamic Route(this object runLog)
        {
            dynamic dynRunLog = runLog as dynamic;
            return new Route().Single(dynRunLog.RouteId);
        }
    }
}

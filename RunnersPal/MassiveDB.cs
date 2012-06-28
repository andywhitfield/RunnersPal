using System;
using System.Collections.Generic;
using System.Linq;

namespace RunnersPal
{
    public class MassiveDB
    {
        public static string ConnectionStringName { get; set; }

        [ThreadStatic]
        private static MassiveDB threadInstance;

        public static MassiveDB Current
        {
            get
            {
                if (threadInstance == null) threadInstance = new MassiveDB();
                return threadInstance;
            }
        }

        static MassiveDB()
        {
            ConnectionStringName = "runnerspal";
        }

        public dynamic FindUser(string openId)
        {
            return new UserAccount().Query("select ua.* from UserAccount ua join UserAccountAuthentication uaa on ua.Id = uaa.UserAccountId where uaa.Identifier = @0", openId).SingleOrDefault();
        }
        public dynamic FindUser(long userId)
        {
            return new UserAccount().Single(userId);
        }
        public void UpdateUser(dynamic userAccount)
        {
            userAccount.LastActivityDate = DateTime.UtcNow;
            new UserAccount().Update(userAccount, userAccount.Id);
        }
        public dynamic CreateUser(string openId, string originalHost, DistanceUnits currentDistanceUnits)
        {
            var userAccount = new UserAccount().Insert(new { DisplayName = "", CreatedDate = DateTime.UtcNow, LastActivityDate = DateTime.UtcNow, OriginalHostAddress = originalHost, UserType = "N", DistanceUnits = currentDistanceUnits });
            new UserAccountAuthentication().Insert(new { UserAccountId = userAccount.Id, Identifier = openId });
            return userAccount;
        }

        public dynamic CreatePref(dynamic userAccount, double? weight, string weightUnits)
        {
            var userPref = new UserPref();
            var userPrefs = userPref.All("UserAccountId=@0 and ValidTo is null", args: new object[] { userAccount.Id }).ToArray();
            foreach (var p in userPrefs) p.ValidTo = DateTime.UtcNow;
            userPref.Save(userPrefs.ToArray());

            return userPref.Insert(new { UserAccountId = userAccount.Id, Weight = weight, WeightUnits = weightUnits });
        }

        public dynamic FindRunLogEvent(long runLogId)
        {
            return new RunLog().Single(runLogId);
        }
        public IEnumerable<dynamic> FindRunLogEvents(dynamic userAccount, bool includeDeletedEvents = true)
        {
            if (userAccount == null) return Enumerable.Empty<dynamic>();

            var whereClause = "UserAccountId = @0";
            if (!includeDeletedEvents) whereClause += " and LogState = 'V'";
            return new RunLog().All(where: whereClause, args: new object[] { userAccount.Id });
        }
        public IEnumerable<dynamic> FindLatestRunLogForRoutes(IEnumerable<long> routeIds, bool includeDeletedEvents = false)
        {
            if (routeIds == null || !routeIds.Any())
                return Enumerable.Empty<dynamic>();

            var query = string.Format(@"select l.*, ua.DisplayName
from (
	select RouteId, max(useraccountid) 'UserAccountId', count(distinct(useraccountid)) 'UserAccountCount', max(date) 'Date'
	from runlog
	where routeid in ({0})
	group by routeid
) l, UserAccount ua
where l.UserAccountId = ua.Id", string.Join(", ", routeIds.Select(id => id.ToString())));
            return new RunLog().Query(query);
        }
        public dynamic CreateRunLogEvent(dynamic userAccount, DateTime runDate, Distance distance, dynamic route, string time, string comment)
        {
            if (route == null)
                route = CreateRoute(userAccount, string.Format("{0} {1}", distance.BaseDistance.ToString("0.##"), distance.BaseUnits), null, distance, 'M');

            return new RunLog().Insert(new { Date = runDate, RouteId = route.Id, TimeTaken = time, UserAccountId = userAccount.Id, CreatedDate = DateTime.UtcNow, Comment = comment ?? "", LogState = 'V' });
        }
        public void UpdateRunLogEvent(dynamic runLogEvent)
        {
            new RunLog().Update(runLogEvent, runLogEvent.Id);
        }

        public IEnumerable<dynamic> GetCommonRoutes()
        {
            return new Route().Query("select r.* from route r join UserAccount ua on r.Creator = ua.Id and ua.DisplayName = 'Admin' and ua.UserType = 'A' and r.RouteType = '" + Route.SystemRoute + "' order by r.Id");
        }
        public dynamic FindRoute(long routeId, bool returnDeleted = false)
        {
            var route = new Route().Single(routeId);
            if (!returnDeleted && (route == null || route.RouteType == Route.DeletedRoute.ToString()))
                return null;
            return route;
        }
        public dynamic FindRoute(string name, bool returnDeleted = false)
        {
            var route = new Route().All(where: "Name = @0", args: name).SingleOrDefault();
            if (!returnDeleted && (route == null || route.RouteType == Route.DeletedRoute.ToString()))
                return null;
            return route;
        }
        public IEnumerable<dynamic> FindRoutes(dynamic userAccount, bool returnDeleted = false)
        {
            if (userAccount == null) return Enumerable.Empty<dynamic>();

            return new Route().All(
                where: "Creator = @0 and RouteType in ('" + Route.PublicRoute + "', '" + Route.PrivateRoute + "'" + (returnDeleted ? ", '" + Route.DeletedRoute + "'" : "") + ")",
                args: new object[] { userAccount.Id });
        }
        public IEnumerable<dynamic> SearchForRoutes(dynamic userAccount, string searchTerm, bool returnDeleted = false)
        {
            IEnumerable<dynamic> routeMatches = new dynamic[0];
            if (string.IsNullOrWhiteSpace(searchTerm)) return routeMatches;
            var query = "%" + searchTerm.ToLower().Replace('*', '%') + "%";
            if (userAccount != null)
            {
                routeMatches = routeMatches.Concat(
                    new Route().All(
                        where: "(lower(Name) like @0 or lower(Notes) like @0) and (Creator = @1 and MapPoints is not null and RouteType in ('" + Route.PublicRoute + "', '" + Route.PrivateRoute + "'" + (returnDeleted ? ", '" + Route.DeletedRoute + "'" : "") + "))",
                        args: new object[] { query, userAccount.Id })
                );
            }

            routeMatches = routeMatches.Concat(
                new Route().All(
                where: "(lower(Name) like @0 or lower(Notes) like @0) and (" + (userAccount != null ? "Creator <> @1 and " : "") + "MapPoints is not null and RouteType = '" + Route.PublicRoute + "')",
                args: new object[] { query, userAccount != null ? userAccount.Id : 0 })
            );

            return routeMatches;
        }
        public dynamic CreateRoute(dynamic creator, string name, string notes, Distance distance, char type, string points = null)
        {
            return new Route().Insert(new { Name = name, Notes = notes, Distance = distance.BaseDistance, DistanceUnits = (int)distance.BaseUnits, Creator = creator.Id, CreatedDate = DateTime.UtcNow, RouteType = type, MapPoints = points });
        }
        public void UpdateRoute(dynamic route)
        {
            new Route().Update(route, route.Id);
        }

        public IEnumerable<dynamic> FindSettings(string domain)
        {
            return new SiteSettings().All(where: "Domain = @0", args: domain);
        }
        public void InsertDomainSetting(string domain, string identifier, string value)
        {
            new SiteSettings().Insert(new { Domain = domain, Identifier = identifier, SettingValue = value });
        }
        public void RemoveDomainSettings(string domain)
        {
            new SiteSettings().Delete(where: "Domain = @0", args: domain);
        }
    }
}

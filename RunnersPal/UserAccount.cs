using System.Collections.Generic;
using System.Linq;
using Massive;
using System;

namespace RunnersPal
{
    public class UserAccount : DynamicModel
    {
        public UserAccount() : base(MassiveDB.ConnectionStringName, "UserAccount", "Id") { }
    }

    public static class UserAccountExtensions
    {
        public static IEnumerable<dynamic> UserAccountAuthentication(this object userAccount)
        {
            dynamic dynUserAccount = userAccount as dynamic;
            return new UserAccountAuthentication().All("UserAccountId=@0", args: new object[] { dynUserAccount.Id });
        }

        /// <returns>All the user pref instances for the specified user account, from oldest to newest.</returns>
        public static IEnumerable<dynamic> UserPrefs(this object userAccount)
        {
            dynamic dynUserAccount = userAccount as dynamic;
            IEnumerable<dynamic> userPrefs = new UserPref().All("UserAccountId=@0", args: new object[] { dynUserAccount.Id });
            return userPrefs.OrderBy(userPref => userPref.ValidTo ?? DateTime.MaxValue);
        }
    }
}

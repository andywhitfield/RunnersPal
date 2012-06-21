using System.Collections.Generic;
using System.Linq;
using Massive;
using System;

namespace RunnersPal
{
    public class UserPref : DynamicModel
    {
        public UserPref() : base(MassiveDB.ConnectionStringName, "UserPref", "Id") { }
    }

    public static class UserPrefExtensions
    {
        public static dynamic Latest(this IEnumerable<dynamic> userPrefs)
        {
            return userPrefs.LastOrDefault();
        }
        public static dynamic Latest(this IEnumerable<dynamic> userPrefs, DateTime ondate)
        {
            var lastValidPref = userPrefs.FirstOrDefault(p => p.ValidTo != null && ((DateTime)p.ValidTo) >= ondate);
            return lastValidPref ?? Latest(userPrefs);
            /*
            dynamic userPrefOnDate = null;
            foreach (var userPref in userPrefs)
            {
                DateTime validTo = userPref.ValidTo == null ? DateTime.UtcNow : (DateTime)userPref.ValidTo;
                if (validTo > ondate) break;
                userPrefOnDate = userPref;
            }
            return userPrefOnDate;
             */
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Massive;

namespace RunnersPal
{
    public class UserAccountAuthentication : DynamicModel
    {
        public UserAccountAuthentication() : base(MassiveDB.ConnectionStringName, "UserAccountAuthentication", "Id") { }
    }
}

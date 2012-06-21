using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Massive;

namespace RunnersPal
{
    public class SiteSettings : DynamicModel
    {
        public SiteSettings() : base(MassiveDB.ConnectionStringName, "SiteSettings", "Id") { }
    }
}

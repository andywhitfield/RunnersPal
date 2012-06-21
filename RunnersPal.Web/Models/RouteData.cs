using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace RunnersPal.Web.Models
{
    public class RouteData
    {
        [Required]
        public long Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Notes { get; set; }
        public bool? Public { get; set; }
        [Required]
        public string Points { get; set; }
        [Required]
        public double Distance { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using ff14bot.Helpers;

namespace Kupo.Settings
{
    internal class NinjaSettings : KupoSettings
    {
        public NinjaSettings(string filename = "Ninja-KupoSettings") : base(filename)
        {
        }


        [Setting]
        [DefaultValue(false)]
        public bool UseAoe { get; set; }

    }



}
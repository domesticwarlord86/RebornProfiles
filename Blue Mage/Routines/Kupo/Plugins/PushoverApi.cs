using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace Kupo.Plugins
{
    internal class PushoverApi
    {
        private const string PUSHOVER_API_URL = "https://api.pushover.net/1/messages.json";

        public enum Sound
        {
            Pushover,
            Bike,
            Bugle,
            CashRegister,
            Classical,
            Cosmic,
            Falling,
            Gamelan,
            Incoming,
            Intermission,
            Magic,
            Mechanical,
            PianoBar,
            Siren,
            SpaceAalarm,
            TugBoat,
            None
        }

        public static void PushNotification(string token, string user, string message, string device = null,
            string title = null,
            string url = null, string urlTitle = null, int priority = 0, Sound sound = Sound.Pushover)
        {
            using (WebClient wc = new WebClient())
            {
                NameValueCollection nvc = new NameValueCollection();

                nvc.Add("token", token);
                nvc.Add("user", user);
                nvc.Add("message", message);

                if (!string.IsNullOrEmpty(device))
                    nvc.Add("device", device);
                if (!string.IsNullOrEmpty(title))
                    nvc.Add("title", title);
                if (!string.IsNullOrEmpty(url))
                    nvc.Add("url", url);
                if (!string.IsNullOrEmpty(urlTitle))
                    nvc.Add("urlTitle", urlTitle);
                if (priority != 0)
                    nvc.Add("priority", priority.ToString(CultureInfo.InvariantCulture));
                if (sound != Sound.Pushover)
                    nvc.Add("sound", sound.ToString().ToLower());

                wc.UploadValues(PUSHOVER_API_URL, nvc);
            }
        }
    }
}
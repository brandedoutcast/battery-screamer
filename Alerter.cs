using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace BatteryScreamer
{
    internal sealed class Alerter
    {
        static readonly Lazy<Alerter> Lazy = new Lazy<Alerter>(() => new Alerter(), true);
        internal static Alerter Instance { get => Lazy.Value; }
        MediaPlayer Player { get; set; }

        private Alerter()
        {
            Player = new MediaPlayer { Looping = true };
            Player.Prepared += (s, e) => Player.Start();
        }

        internal void Play(string alertName)
        {
            Player.SetDataSource($"android.resource://com.androidbook.samplevideo/raw/{alertName}");
            Player.Prepare();
        }

        internal void Stop()
        {
            Player.Stop();
        }
    }
}
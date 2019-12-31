using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Widget;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BatteryScreamer
{
    [Activity(Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        const int JOB_ID = 41383;

        TextView Info, PSource, CStatus;
        Switch JobToggle;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.main);

            Info = FindViewById<TextView>(Resource.Id.info);
            PSource = FindViewById<TextView>(Resource.Id.psource);
            CStatus = FindViewById<TextView>(Resource.Id.cstatus);

            JobToggle = FindViewById<Switch>(Resource.Id.jstatus);

            Init();
        }

        void Init()
        {
            JobToggle.Click += delegate { if (JobToggle.Checked) ScheduleJob(); else CancelJob(); };

            UpdateInfo();
            Battery.BatteryInfoChanged += delegate { UpdateInfo(); };

            UpdateJobStatus();
            Task.Run(() =>
            {
                do
                {
                    UpdateJobStatus();
                    Task.Delay(30000).Wait();
                } while (true);
            });
        }

        void UpdateInfo()
        {
            PSource.Text = Battery.PowerSource.ToString();
            CStatus.Text = Battery.State.ToString();
            Info.Text = $"{Battery.ChargeLevel * 100}%";
        }

        void UpdateJobStatus(bool? isActive = null)
        {
            if (!isActive.HasValue)
                isActive = ((JobScheduler)GetSystemService(JobSchedulerService)).AllPendingJobs.Any(j => j.Id == JOB_ID);

            JobToggle.Checked = isActive.Value;
        }

        void ScheduleJob()
        {
            var JavaClass = Java.Lang.Class.FromType(typeof(BatteryScreamerJob));
            var Component = new ComponentName(this, JavaClass);
            var JobInfo = new JobInfo.Builder(JOB_ID, Component)
                                .SetRequiredNetworkType(NetworkType.Unmetered)
                                .SetRequiresDeviceIdle(true)
                                .SetPersisted(true)
                                .SetPeriodic(15 * 60 * 1000)
                                .Build();

            var Scheduler = (JobScheduler)GetSystemService(JobSchedulerService);
            var IsScheduled = Scheduler.Schedule(JobInfo) == JobScheduler.ResultSuccess;
            UpdateJobStatus(IsScheduled);
        }

        void CancelJob()
        {
            var Scheduler = (JobScheduler)GetSystemService(JobSchedulerService);
            Scheduler.Cancel(JOB_ID);
            UpdateJobStatus(false);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
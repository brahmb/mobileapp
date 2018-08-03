using System;
using Realms;
using Toggl.PrimeRadiant.Models;

namespace Toggl.PrimeRadiant.Realm
{
    internal partial class RealmWorkspace : RealmObject, IDatabaseWorkspace
    {
        public string Name { get; set; }

        public bool Admin { get; set; }

        public DateTimeOffset? SuspendedAt { get; set; }

        public DateTimeOffset? ServerDeletedAt { get; set; }

        public double? DefaultHourlyRate { get; set; }

        public string DefaultCurrency { get; set; }

        public bool OnlyAdminsMayCreateProjects { get; set; }

        public bool OnlyAdminsSeeBillableRates { get; set; }

        public bool OnlyAdminsSeeTeamDashboard { get; set; }

        public bool ProjectsBillableByDefault { get; set; }

        public int Rounding { get; set; }

        public int RoundingMinutes { get; set; }

        public DateTimeOffset At { get; set; }

        public string LogoUrl { get; set; }

        public bool IsGhost { get; set; }
    }
}

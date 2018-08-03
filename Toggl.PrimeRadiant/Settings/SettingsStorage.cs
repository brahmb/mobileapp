using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Onboarding;

namespace Toggl.PrimeRadiant.Settings
{
    public sealed class SettingsStorage : IAccessRestrictionStorage, IOnboardingStorage, IUserPreferences, ILastTimeUsageStorage
    {
        private const string outdatedApiKey = "OutdatedApi";
        private const string outdatedClientKey = "OutdatedClient";
        private const string unauthorizedAccessKey = "UnauthorizedAccessForApiToken";

        private const string userSignedUpUsingTheAppKey = "UserSignedUpUsingTheApp";
        private const string isNewUserKey = "IsNewUser";
        private const string lastAccessDateKey = "LastAccessDate";
        private const string firstAccessDateKey = "FirstAccessDate";
        private const string completedOnboardingKey = "CompletedOnboarding";

        private const string preferManualModeKey = "PreferManualMode";

        private const string startButtonWasTappedBeforeKey = "StartButtonWasTappedBefore";
        private const string hasTappedTimeEntryKey = "HasTappedTimeEntry";
        private const string hasEditedTimeEntryKey = "HasEditedTimeEntry";
        private const string projectOrTagWasAddedBeforeKey = "ProjectOrTagWasAddedBefore";
        private const string stopButtonWasTappedBeforeKey = "StopButtonWasTappedBefore";
        private const string hasSelectedProjectKey = "HasSelectedProject";
        private const string navigatedAwayFromMainViewAfterTappingStopButtonKey = "NavigatedAwayFromMainView";
        private const string hasTimeEntryBeenContinuedKey = "HasTimeEntryBeenContinued";

        private const string onboardingPrefix = "Onboarding_";

        private const string ratingViewOutcomeKey = "RatingViewOutcome";
        private const string ratingViewOutcomeTimeKey = "RatingViewOutcomeTime";
        private const string ratingViewNumberOfTimesShownKey = "RatingViewNumberOfTimesShown";

        private const string lastSyncAttemptKey = "LastSyncAttempt";
        private const string lastSuccessfulSyncKey = "LastSuccessfulSync";
        private const string lastLoginKey = "LastLogin";

        private readonly Version version;
        private readonly IKeyValueStorage keyValueStorage;

        private readonly ISubject<bool> userSignedUpUsingTheAppSubject;
        private readonly ISubject<bool> isNewUserSubject;
        private readonly ISubject<bool> projectOrTagWasAddedSubject;
        private readonly ISubject<bool> startButtonWasTappedSubject;
        private readonly ISubject<bool> hasTappedTimeEntrySubject;
        private readonly ISubject<bool> hasEditedTimeEntrySubject;
        private readonly ISubject<bool> stopButtonWasTappedSubject;
        private readonly ISubject<bool> hasSelectedProjectSubject;
        private readonly ISubject<bool> isManualModeEnabledSubject;
        private readonly ISubject<bool> navigatedAwayFromMainViewAfterTappingStopButtonSubject;
        private readonly ISubject<bool> hasTimeEntryBeenContinuedSubject;

        public SettingsStorage(Version version, IKeyValueStorage keyValueStorage)
        {
            Ensure.Argument.IsNotNull(keyValueStorage, nameof(keyValueStorage));

            this.version = version;
            this.keyValueStorage = keyValueStorage;

            (isNewUserSubject, IsNewUser) = prepareSubjectAndObservable(isNewUserKey);
            (isManualModeEnabledSubject, IsManualModeEnabledObservable) = prepareSubjectAndObservable(preferManualModeKey);
            (hasTappedTimeEntrySubject, HasTappedTimeEntry) = prepareSubjectAndObservable(hasTappedTimeEntryKey);
            (hasEditedTimeEntrySubject, HasEditedTimeEntry) = prepareSubjectAndObservable(hasEditedTimeEntryKey);
            (hasSelectedProjectSubject, HasSelectedProject) = prepareSubjectAndObservable(hasSelectedProjectKey);
            (stopButtonWasTappedSubject, StopButtonWasTappedBefore) = prepareSubjectAndObservable(stopButtonWasTappedBeforeKey);
            (userSignedUpUsingTheAppSubject, UserSignedUpUsingTheApp) = prepareSubjectAndObservable(userSignedUpUsingTheAppKey);
            (startButtonWasTappedSubject, StartButtonWasTappedBefore) = prepareSubjectAndObservable(startButtonWasTappedBeforeKey);
            (projectOrTagWasAddedSubject, ProjectOrTagWasAddedBefore) = prepareSubjectAndObservable(projectOrTagWasAddedBeforeKey);
            (navigatedAwayFromMainViewAfterTappingStopButtonSubject, NavigatedAwayFromMainViewAfterTappingStopButton) = prepareSubjectAndObservable(navigatedAwayFromMainViewAfterTappingStopButtonKey);
            (hasTimeEntryBeenContinuedSubject, HasTimeEntryBeenContinued) = prepareSubjectAndObservable(hasTimeEntryBeenContinuedKey);
        }

        #region IAccessRestrictionStorage

        public void SetClientOutdated()
        {
            keyValueStorage.SetString(outdatedClientKey, version.ToString());
        }

        public void SetApiOutdated()
        {
            keyValueStorage.SetString(outdatedApiKey, version.ToString());
        }

        public void SetUnauthorizedAccess(string apiToken)
        {
            keyValueStorage.SetString(unauthorizedAccessKey, apiToken);
        }

        public bool IsClientOutdated()
            => isOutdated(outdatedClientKey);

        public bool IsApiOutdated()
            => isOutdated(outdatedApiKey);

        public bool IsUnauthorized(string apiToken)
            => apiToken == keyValueStorage.GetString(unauthorizedAccessKey);

        private bool isOutdated(string key)
        {
            var storedVersion = getStoredVersion(key);
            return storedVersion != null && version <= storedVersion;
        }

        private Version getStoredVersion(string key)
        {
            var stored = keyValueStorage.GetString(key);
            return stored == null ? null : Version.Parse(stored);
        }

        #endregion

        #region IOnboardingStorage

        public IObservable<bool> UserSignedUpUsingTheApp { get; }

        public IObservable<bool> IsNewUser { get; }

        public IObservable<bool> StartButtonWasTappedBefore { get; }

        public IObservable<bool> HasTappedTimeEntry { get; }

        public IObservable<bool> HasEditedTimeEntry { get; }

        public IObservable<bool> ProjectOrTagWasAddedBefore { get; }

        public IObservable<bool> StopButtonWasTappedBefore { get; }

        public IObservable<bool> HasSelectedProject { get; }

        public IObservable<bool> NavigatedAwayFromMainViewAfterTappingStopButton { get; }

        public IObservable<bool> HasTimeEntryBeenContinued { get; }

        public void SetLastOpened(DateTimeOffset date)
        {
            var dateString = date.ToString();
            keyValueStorage.SetString(lastAccessDateKey, dateString);
        }

        public void SetFirstOpened(DateTimeOffset dateTime)
        {
            if (GetFirstOpened() == null)
                keyValueStorage.SetString(firstAccessDateKey, dateTime.ToString());
        }

        public void SetUserSignedUp()
        {
            userSignedUpUsingTheAppSubject.OnNext(true);
            keyValueStorage.SetBool(userSignedUpUsingTheAppKey, true);
        }

        public void SetNavigatedAwayFromMainViewAfterStopButton()
        {
            navigatedAwayFromMainViewAfterTappingStopButtonSubject.OnNext(true);
            keyValueStorage.SetBool(navigatedAwayFromMainViewAfterTappingStopButtonKey, true);
        }

        public void SetTimeEntryContinued()
        {
            hasTimeEntryBeenContinuedSubject.OnNext(true);
            keyValueStorage.SetBool(hasTimeEntryBeenContinuedKey, true);
        }

        public void SetIsNewUser(bool isNewUser)
        {
            isNewUserSubject.OnNext(isNewUser);
            keyValueStorage.SetBool(isNewUserKey, isNewUser);
        }

        public void SetCompletedOnboarding()
        {
            keyValueStorage.SetBool(completedOnboardingKey, true);
        }

        public bool CompletedOnboarding() => keyValueStorage.GetBool(completedOnboardingKey);

        public string GetLastOpened() => keyValueStorage.GetString(lastAccessDateKey);

        public DateTimeOffset? GetFirstOpened()
        {
            var dateString = keyValueStorage.GetString(firstAccessDateKey);

            if (string.IsNullOrEmpty(dateString))
                return null;

            return DateTimeOffset.Parse(dateString);
        }

        public void StartButtonWasTapped()
        {
            startButtonWasTappedSubject.OnNext(true);
            keyValueStorage.SetBool(startButtonWasTappedBeforeKey, true);
        }

        public void TimeEntryWasTapped()
        {
            hasTappedTimeEntrySubject.OnNext(true);
            keyValueStorage.SetBool(hasTappedTimeEntryKey, true);
        }

        public void ProjectOrTagWasAdded()
        {
            projectOrTagWasAddedSubject.OnNext(true);
            keyValueStorage.SetBool(projectOrTagWasAddedBeforeKey, true);
        }

        public void StopButtonWasTapped()
        {
            stopButtonWasTappedSubject.OnNext(true);
            keyValueStorage.SetBool(stopButtonWasTappedBeforeKey, true);
        }

        public void SelectsProject()
        {
            hasSelectedProjectSubject.OnNext(true);
            keyValueStorage.SetBool(hasSelectedProjectKey, true);
        }

        public void EditedTimeEntry()
        {
            hasEditedTimeEntrySubject.OnNext(true);
            keyValueStorage.SetBool(hasEditedTimeEntryKey, true);
        }

        public void SetDidShowRatingView()
        {
            keyValueStorage.SetInt(ratingViewNumberOfTimesShownKey, NumberOfTimesRatingViewWasShown() + 1);
        }

        public int NumberOfTimesRatingViewWasShown()
        {
            return keyValueStorage.GetInt(ratingViewOutcomeKey, 0);
        }

        public void SetRatingViewOutcome(RatingViewOutcome outcome, DateTimeOffset dateTime)
        {
            keyValueStorage.SetInt(ratingViewOutcomeKey, (int)outcome);
            keyValueStorage.SetDateTimeOffset(ratingViewOutcomeTimeKey, dateTime);
        }

        public RatingViewOutcome? RatingViewOutcome()
        {
            var defaultIntValue = -1;
            var intValue = keyValueStorage.GetInt(ratingViewOutcomeKey, defaultIntValue);
            if (intValue == defaultIntValue)
                return null;
            return (RatingViewOutcome)intValue;
        }

        public DateTimeOffset? RatingViewOutcomeTime()
            => keyValueStorage.GetDateTimeOffset(ratingViewOutcomeTimeKey);

        public bool WasDismissed(IDismissable dismissable) => keyValueStorage.GetBool(onboardingPrefix + dismissable.Key);

        public void Dismiss(IDismissable dismissable) => keyValueStorage.SetBool(onboardingPrefix + dismissable.Key, true);

        void IOnboardingStorage.Reset()
        {
            keyValueStorage.SetBool(startButtonWasTappedBeforeKey, false);
            startButtonWasTappedSubject.OnNext(false);

            keyValueStorage.SetBool(hasTappedTimeEntryKey, false);
            hasTappedTimeEntrySubject.OnNext(false);

            keyValueStorage.SetBool(userSignedUpUsingTheAppKey, false);
            userSignedUpUsingTheAppSubject.OnNext(false);

            keyValueStorage.SetBool(hasEditedTimeEntryKey, false);
            hasEditedTimeEntrySubject.OnNext(false);

            keyValueStorage.SetBool(stopButtonWasTappedBeforeKey, false);
            stopButtonWasTappedSubject.OnNext(false);

            keyValueStorage.SetBool(projectOrTagWasAddedBeforeKey, false);
            projectOrTagWasAddedSubject.OnNext(false);

            keyValueStorage.SetBool(navigatedAwayFromMainViewAfterTappingStopButtonKey, false);
            navigatedAwayFromMainViewAfterTappingStopButtonSubject.OnNext(false);

            keyValueStorage.SetBool(hasTimeEntryBeenContinuedKey, false);
            hasTimeEntryBeenContinuedSubject.OnNext(false);

            keyValueStorage.RemoveAllWithPrefix(onboardingPrefix);
        }

        #endregion

        #region IUserPreferences

        public IObservable<bool> IsManualModeEnabledObservable { get; }

        public bool IsManualModeEnabled
            => keyValueStorage.GetBool(preferManualModeKey);

        public void EnableManualMode()
        {
            keyValueStorage.SetBool(preferManualModeKey, true);
            isManualModeEnabledSubject.OnNext(true);
        }

        public void EnableTimerMode()
        {
            keyValueStorage.SetBool(preferManualModeKey, false);
            isManualModeEnabledSubject.OnNext(false);
        }

        void IUserPreferences.Reset()
        {
            EnableTimerMode();
            isManualModeEnabledSubject.OnNext(false);
        }

        #endregion

        #region ILastTimeUsageStorage

        public DateTimeOffset? LastSyncAttempt => keyValueStorage.GetDateTimeOffset(lastSyncAttemptKey);

        public DateTimeOffset? LastSuccessfulSync => keyValueStorage.GetDateTimeOffset(lastSuccessfulSyncKey);

        public DateTimeOffset? LastLogin => keyValueStorage.GetDateTimeOffset(lastLoginKey);


        public void SetFullSyncAttempt(DateTimeOffset now)
        {
            keyValueStorage.SetDateTimeOffset(lastSyncAttemptKey, now);
        }

        public void SetSuccessfulFullSync(DateTimeOffset now)
        {
            keyValueStorage.SetDateTimeOffset(lastSuccessfulSyncKey, now);
        }

        public void SetLogin(DateTimeOffset now)
        {
            keyValueStorage.SetDateTimeOffset(lastLoginKey, now);
        }

        #endregion

        private (ISubject<bool>, IObservable<bool>) prepareSubjectAndObservable(string key)
        {
            var initialValue = keyValueStorage.GetBool(key);
            var subject = new BehaviorSubject<bool>(initialValue);
            var observable = subject.AsObservable().DistinctUntilChanged();

            return (subject, observable);
        }
    }
}

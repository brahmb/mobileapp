using System;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Toggl.Multivac.Extensions;
using Xunit;

namespace Toggl.Multivac.Tests
{
    public sealed class RxActionTests
    {
        public sealed class TheErrorsProperty : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void ForwardsErrorsInTheExecution()
            {
                var testScheduler = new TestScheduler();
                var exception = new Exception("This is an error");

                var action = new RxAction<int, string>(
                    i => Observable.Throw<string>(exception)
                );

                var observer = testScheduler.CreateObserver<Exception>();

                action.Errors.Subscribe(observer);

                testScheduler.Sleep(3);
                action.Execute(2);
                testScheduler.Start();

                observer.Messages.AssertEqual(
                    OnNext(3, exception)
                );
            }
        }

        public sealed class TheExecutingProperty : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsTrueWhileExecuting()
            {
                var testScheduler = new TestScheduler();

                var action = new RxAction<int, string>(
                    i => Observable.Interval(TimeSpan.FromTicks(1), testScheduler)
                        .Take(i)
                        .Select(l => l.ToString())
                );

                var observer = testScheduler.CreateObserver<bool>();

                action.Executing.Subscribe(observer);

                testScheduler.Sleep(3);
                action.Execute(2);
                testScheduler.Start();

                observer.Messages.AssertEqual(
                    OnNext(0, false),
                    OnNext(3, true),
                    OnNext(5, false)
                );
            }
        }

        public sealed class TheEnabledProperty : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsFalseWhileExecuting()
            {
                var testScheduler = new TestScheduler();

                var action = new RxAction<int, string>(
                    i => Observable.Interval(TimeSpan.FromTicks(1), testScheduler)
                        .Take(i)
                        .Select(l => l.ToString())
                );

                var observer = testScheduler.CreateObserver<bool>();

                action.Enabled.Subscribe(observer);

                testScheduler.Sleep(3);
                action.Execute(2);
                testScheduler.Start();

                observer.Messages.AssertEqual(
                    OnNext(0, true),
                    OnNext(3, false),
                    OnNext(5, true)
                );
            }
        }

        public sealed class TheExecuteMethod : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsTheResultsOfTheOperation()
            {
                var testScheduler = new TestScheduler();

                var action = new RxAction<int, string>(
                   i => Observable.Interval(TimeSpan.FromTicks(1), testScheduler)
                        .Take(i)
                        .Select(l => l.ToString())
                );

                var observer = testScheduler.CreateObserver<string>();

                testScheduler.Sleep(3);
                action.Execute(2).Subscribe(observer);
                testScheduler.Start();

                observer.Messages.AssertEqual(
                    OnNext(4, "0"),
                    OnNext(5, "1"),
                    OnCompleted<string>(5)
                );
            }
        }
    }
}

using System;
using FluentAssertions;
using MvvmCross.Navigation;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.Services
{
    public sealed class ErrorHandlingServiceTests
    {
        public abstract class BaseErrorHandlingServiceTests
        {
            protected readonly IMvxNavigationService NavigationService;
            protected readonly IAccessRestrictionStorage AccessRestrictionStorage;
            protected readonly IErrorHandlingService ErrorHandlingService;
            protected readonly IDatabaseUser User;

            public BaseErrorHandlingServiceTests()
            {
                User = Substitute.For<IDatabaseUser>();
                var token = Guid.NewGuid().ToString();
                User.ApiToken.Returns(token);
                NavigationService = Substitute.For<IMvxNavigationService>();
                AccessRestrictionStorage = Substitute.For<IAccessRestrictionStorage>();
                ErrorHandlingService =
                    new ErrorHandlingService(NavigationService, AccessRestrictionStorage);
            }
        }

        public sealed class TheConstructor
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useNavigationService, bool useAccessRestrictionStorage)
            {
                var navigationService = useNavigationService ? Substitute.For<IMvxNavigationService>() : null;
                var accessRestrictionStorage = useAccessRestrictionStorage ? Substitute.For<IAccessRestrictionStorage>() : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new ErrorHandlingService(navigationService, accessRestrictionStorage);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheApiDeprecatedException : BaseErrorHandlingServiceTests
        {
            private ApiDeprecatedException exception => new ApiDeprecatedException(Substitute.For<IRequest>(), Substitute.For<IResponse>());

            [Fact, LogIfTooSlow]
            public void ReturnsTrueForApiDeprecatedException()
            {
                var result = ErrorHandlingService.TryHandleDeprecationError(exception);

                result.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void RestricsAccessForApiDeprecatedException()
            {
                ErrorHandlingService.TryHandleDeprecationError(exception);

                AccessRestrictionStorage.Received().SetApiOutdated();
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToOutdatedAppViewModelForApiDeprecatedException()
            {
                ErrorHandlingService.TryHandleDeprecationError(exception);

                NavigationService.Received().Navigate<OutdatedAppViewModel>();
            }
        }

        public sealed class TheClientDeprecatedException : BaseErrorHandlingServiceTests
        {
            private ClientDeprecatedException exception => new ClientDeprecatedException(Substitute.For<IRequest>(), Substitute.For<IResponse>());

            [Fact, LogIfTooSlow]
            public void ReturnsTrueForClientDeprecatedException()
            {
                var result = ErrorHandlingService.TryHandleDeprecationError(exception);

                result.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void RestricsAccessForClientDeprecatedException()
            {
                ErrorHandlingService.TryHandleDeprecationError(exception);

                AccessRestrictionStorage.Received().SetClientOutdated();
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToOutdatedAppViewModelForClientDeprecatedException()
            {
                ErrorHandlingService.TryHandleDeprecationError(exception);

                NavigationService.Received().Navigate<OutdatedAppViewModel>();
            }
        }

        public sealed class TheOtherExceptionsThanDeprecationExceptions : BaseErrorHandlingServiceTests
        {
            private Exception exception => new Exception();

            [Fact, LogIfTooSlow]
            public void ReturnsFalseForDifferentExceptions()
            {
                ErrorHandlingService.TryHandleDeprecationError(exception);

                NavigationService.DidNotReceive().Navigate<OutdatedAppViewModel>();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotRestrictAccessForDifferentExceptions()
            {
                ErrorHandlingService.TryHandleDeprecationError(exception);

                AccessRestrictionStorage.DidNotReceive().SetApiOutdated();
                AccessRestrictionStorage.DidNotReceive().SetClientOutdated();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotNavigateForDifferentExceptions()
            {
                ErrorHandlingService.TryHandleDeprecationError(exception);

                NavigationService.DidNotReceive().Navigate<OutdatedAppViewModel>();
            }
        }

        public sealed class TheUnauthorizedException : BaseErrorHandlingServiceTests
        {
            private UnauthorizedException exception => new UnauthorizedException(createRequest(), Substitute.For<IResponse>());

            private IRequest createRequest()
            {
                var request = Substitute.For<IRequest>();
                var headers = new[] { Credentials.WithApiToken(User.ApiToken).Header };
                request.Headers.Returns(headers);
                return request;
            }

            [Fact, LogIfTooSlow]
            public void ReturnsTrueForClientDeprecatedException()
            {
                var result = ErrorHandlingService.TryHandleUnauthorizedError(exception);

                result.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void RestricsAccessForClientDeprecatedException()
            {
                ErrorHandlingService.TryHandleUnauthorizedError(exception);

                AccessRestrictionStorage.Received().SetUnauthorizedAccess(Arg.Is(User.ApiToken));
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToOutdatedAppViewModelForClientDeprecatedException()
            {
                ErrorHandlingService.TryHandleUnauthorizedError(exception);

                NavigationService.Received().Navigate<TokenResetViewModel>();
            }

            [Fact, LogIfTooSlow]
            internal void ReturnsTrueButDoesNotNavigateOrSetUnathorizedAccessFlagWhenTheApiTokenCannotBeExtractedFromTheRequest()
            {
                var request = Substitute.For<IRequest>();
                request.Headers.Returns(new HttpHeader[0]);
                var exceptionWithoutApiToken = new UnauthorizedException(request, Substitute.For<IResponse>());

                var handled = ErrorHandlingService.TryHandleUnauthorizedError(exceptionWithoutApiToken);

                handled.Should().BeTrue();
                NavigationService.DidNotReceive().Navigate<TokenResetViewModel>();
                AccessRestrictionStorage.DidNotReceive().SetUnauthorizedAccess(Arg.Any<string>());
            }
        }

        public sealed class TheOtherExceptionsThanUnauthorizedException : BaseErrorHandlingServiceTests
        {
            private Exception exception => new Exception();

            [Fact, LogIfTooSlow]
            public void ReturnsFalseForDifferentExceptions()
            {
                ErrorHandlingService.TryHandleUnauthorizedError(exception);

                NavigationService.DidNotReceive().Navigate<TokenResetViewModel>();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotRestrictAccessForDifferentExceptions()
            {
                ErrorHandlingService.TryHandleUnauthorizedError(exception);

                AccessRestrictionStorage.DidNotReceive().SetUnauthorizedAccess(Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public void DoesNotNavigateForDifferentExceptions()
            {
                ErrorHandlingService.TryHandleUnauthorizedError(exception);

                NavigationService.DidNotReceive().Navigate<TokenResetViewModel>();
            }
        }
    }
}

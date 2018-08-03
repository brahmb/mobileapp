using FluentAssertions;
using FsCheck.Xunit;
using Xunit;

namespace Toggl.Multivac.Tests
{
    public sealed class EmailTests
    {
        public sealed class TheIsValidProperty
        {
            [Fact, LogIfTooSlow]
            public void ReturnsFalseIfTheEmailWasCreatedUsingTheDefaultConstructor()
            {
                var email = new Email();

                email.IsValid.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsFalseIfTheEmailIsTheSameInstanceAsTheEmptyStaticProperty()
            {
                var email = Email.Empty;

                email.IsValid.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsFalseForAnEmailCreatedWithAnInvalidEmailString()
            {
                var email = Email.From("foo@");

                email.IsValid.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsTrueForAProperlyInitializedValidEmail()
            {
                var email = Email.From("susancalvin@psychohistorian.museum");

                email.IsValid.Should().BeTrue();
            }
        }

        public sealed class TheToStringMethod
        {
            [Fact, LogIfTooSlow]
            public void ReturnsEmptyStringWhenEmailIsEmpty()
            {
                var email = Email.Empty;

                email.ToString().Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNullForAnEmailCreatedWithTheDefaultConstructor()
            {
                var email = new Email();

                email.ToString().Should().BeNull();
            }

            [Property]
            public void ReturnsTheStringUsedForConstruction(string emailString)
            {
                var email = Email.From(emailString);

                email.ToString().Should().Be(emailString);
            }
        }

        public sealed class TheTrimmedEndMethod
        {
            [Theory, LogIfTooSlow]
            [InlineData(" email@with.spaceBothSides ", " email@with.spaceBothSides")]
            [InlineData(" email@with.spaceFront", " email@with.spaceFront")]
            [InlineData("email@with.spaceBack ", "email@with.spaceBack")]
            public void ReturnsNewEmailWithTrimmedEndSpaces(string originalEmail, string expectedEmail)
            {
                var email = Email.From(originalEmail).TrimmedEnd();

                email.ToString().Should().Be(expectedEmail);
            }
        }
    }
}

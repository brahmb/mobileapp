namespace Toggl.Multivac
{
    public struct Password
    {
        private const int minimumLength = 6;

        private readonly string password;

        public bool IsValid { get; }

        public int Length => password.Length;

        private Password(string password)
        {
            this.password = password;
            IsValid = password?.Length >= minimumLength;
        }

        public override string ToString() => password;

        public static Password From(string password)
            => new Password(password);

        public static Password Empty { get; } = new Password("");
    }
}

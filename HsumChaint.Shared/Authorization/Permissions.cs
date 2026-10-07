namespace HsumChaint.Shared.Authorization;

public static class Permissions
{
    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Manage = "Roles.Manage";
        public const string Assign = "Roles.Assign";
    }
    public static class Users
    {
        public const string View = "Users.View";
        public const string Manage = "Users.Manage";
    }
    public static class Monastery
    {
        public const string Create = "Monastery.Create";
        public const string View = "Monastery.View";
        public const string Update = "Monastery.Update";
        public const string Delete = "Monastery.Delete";
        public const string ManageMembers = "Monastery.ManageMembers";
    }
    public static class Donation
    {
        public const string View = "Donation.View";
        public const string Create = "Donation.Create";
        public const string Review = "Donation.Review";
        public const string Schedule = "Donation.Schedule";
        public const string Cancel = "Donation.Cancel";
    }

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[]
    {
        Roles.View, Roles.Manage, Roles.Assign, Users.View, Users.Manage,
        Monastery.Create, Monastery.View, Monastery.Update, Monastery.Delete, Monastery.ManageMembers,
        Donation.View, Donation.Create, Donation.Review, Donation.Schedule, Donation.Cancel
    });
}

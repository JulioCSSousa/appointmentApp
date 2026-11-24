namespace appointmentApp.Constants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "Client";
        public const string Worker = "Worker";
        public const string SuperAdmin = "SuperAdmin";

        public static readonly string[] AllRoles = { Admin, User, Worker, SuperAdmin };
    }

}

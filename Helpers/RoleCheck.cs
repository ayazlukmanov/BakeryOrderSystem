namespace BakeryOrderSystem.Helpers
{
    public static class RoleCheck
    {
        public static bool IsAdmin(string role)
        {
            return role == "Администратор";
        }

        public static bool IsManager(string role)
        {
            return role == "Менеджер";
        }

        public static bool IsBaker(string role)
        {
            return role == "Пекарь";
        }

        public static bool IsAdminOrManager(string role)
        {
            return role == "Администратор" || role == "Менеджер";
        }
    }
}
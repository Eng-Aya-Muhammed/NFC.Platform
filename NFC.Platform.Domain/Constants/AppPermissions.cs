using System.Collections.Generic;
using System.Reflection;

namespace NFC.Platform.Domain.Constants
{
    public static class AppPermissions
    {
        public static class Employees
        {
            public const string View   = "Employees.View";
            public const string Create = "Employees.Create";
            public const string Update = "Employees.Update";
            public const string Delete = "Employees.Delete";
            public const string Import = "Employees.Import";
        }

        public static class CardOrders
        {
            public const string View   = "CardOrders.View";
            public const string Create = "CardOrders.Create";
            public const string Update = "CardOrders.Update";
        }

        public static class Roles
        {
            public const string View         = "Roles.View";
            public const string Create       = "Roles.Create";
            public const string Update       = "Roles.Update";
            public const string Delete       = "Roles.Delete";
            public const string AssignToUser = "Roles.AssignToUser";
        }

        public static class Analytics
        {
            public const string View = "Analytics.View";
        }

        public static class Profiles
        {
            public const string View   = "Profiles.View";
            public const string Update = "Profiles.Update";
        }

        public static class Templates
        {
            public const string View    = "Templates.View";
            public const string Request = "Templates.Request";
        }

        public static class Company
        {
            public const string View   = "Company.View";
            public const string Update = "Company.Update";
        }

        public static IEnumerable<string> GetAll()
        {
            foreach (var nested in typeof(AppPermissions).GetNestedTypes())
            {
                foreach (var field in nested.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    if (field.IsLiteral && !field.IsInitOnly && field.GetRawConstantValue() is string val)
                        yield return val;
                }
            }
        }
    }
}

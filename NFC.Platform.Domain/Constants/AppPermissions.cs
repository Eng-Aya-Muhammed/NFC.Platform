using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NFC.Platform.Domain.Constants
{
    public static class AppPermissions
    {
        // ==========================================
        // TENANT PERMISSIONS (Company / Tenant Level)
        // ==========================================
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
            public const string Cancel = "CardOrders.Cancel";
        }

        public static class Analytics
        {
            public const string View = "Analytics.View";
        }

        public static class Company
        {
            public const string View   = "Company.View";
            public const string Update = "Company.Update";
        }

        public static class Subscriptions
        {
            public const string View   = "Subscriptions.View";
            public const string Update = "Subscriptions.Update";
        }

        public static class TemplateRequests
        {
            public const string View   = "TemplateRequests.View";
            public const string Create = "TemplateRequests.Create";
            public const string Update = "TemplateRequests.Update";
            public const string Cancel = "TemplateRequests.Cancel";
        }

        public static class Profiles
        {
            public const string View   = "Profiles.View";
            public const string Update = "Profiles.Update";
        }

        // ==========================================
        // PLATFORM PERMISSIONS (System Admin Level)
        // ==========================================
        public static class Platform
        {
            public static class Roles
            {
                public const string View         = "Platform.Roles.View";
                public const string Create       = "Platform.Roles.Create";
                public const string Update       = "Platform.Roles.Update";
                public const string Delete       = "Platform.Roles.Delete";
                public const string AssignToUser = "Platform.Roles.AssignToUser";
            }

            public static class Tenants
            {
                public const string View               = "Platform.Tenants.View";
                public const string UpdateStatus       = "Platform.Tenants.UpdateStatus";
                public const string ExtendSubscription = "Platform.Tenants.ExtendSubscription";
            }

            public static class SubscriptionPlans
            {
                public const string View           = "Platform.SubscriptionPlans.View";
                public const string Create         = "Platform.SubscriptionPlans.Create";
                public const string Update         = "Platform.SubscriptionPlans.Update";
                public const string Delete         = "Platform.SubscriptionPlans.Delete";
                public const string AssignTemplate = "Platform.SubscriptionPlans.AssignTemplate";
            }

            public static class CardTypes
            {
                public const string View   = "Platform.CardTypes.View";
                public const string Create = "Platform.CardTypes.Create";
                public const string Update = "Platform.CardTypes.Update";
                public const string Delete = "Platform.CardTypes.Delete";
            }

            public static class CardPackages
            {
                public const string View   = "Platform.CardPackages.View";
                public const string Create = "Platform.CardPackages.Create";
                public const string Update = "Platform.CardPackages.Update";
                public const string Delete = "Platform.CardPackages.Delete";
            }

            public static class TemplateCategories
            {
                public const string View   = "Platform.TemplateCategories.View";
                public const string Create = "Platform.TemplateCategories.Create";
                public const string Update = "Platform.TemplateCategories.Update";
                public const string Delete = "Platform.TemplateCategories.Delete";
            }

            public static class CardTemplates
            {
                public const string View   = "Platform.CardTemplates.View";
                public const string Create = "Platform.CardTemplates.Create";
                public const string Update = "Platform.CardTemplates.Update";
                public const string Delete = "Platform.CardTemplates.Delete";
            }

            public static class DiscountCodes
            {
                public const string View   = "Platform.DiscountCodes.View";
                public const string Create = "Platform.DiscountCodes.Create";
                public const string Update = "Platform.DiscountCodes.Update";
                public const string Delete = "Platform.DiscountCodes.Delete";
            }

            public static class Orders
            {
                public const string View         = "Platform.Orders.View";
                public const string UpdateStatus = "Platform.Orders.UpdateStatus";
                public const string VerifyOtp    = "Platform.Orders.VerifyOtp";
                public const string ResendOtp    = "Platform.Orders.ResendOtp";
            }

            public static class TemplateRequests
            {
                public const string View    = "Platform.TemplateRequests.View";
                public const string Resolve = "Platform.TemplateRequests.Resolve";
            }

            public static class VipCustomers
            {
                public const string View   = "Platform.VipCustomers.View";
                public const string Update = "Platform.VipCustomers.Update";
            }

            public static class Users
            {
                public const string Create = "Platform.Users.Create";
            }
        }

        public static IEnumerable<string> GetTenantPermissions()
        {
            var tenantTypes = new[]
            {
                typeof(Employees),
                typeof(CardOrders),
                typeof(Analytics),
                typeof(Company),
                typeof(Subscriptions),
                typeof(TemplateRequests),
                typeof(Profiles)
            };

            foreach (var type in tenantTypes)
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    if (field.IsLiteral && !field.IsInitOnly && field.GetRawConstantValue() is string val)
                        yield return val;
                }
            }
        }

        public static IEnumerable<string> GetPlatformPermissions()
        {
            foreach (var nested in typeof(Platform).GetNestedTypes())
            {
                foreach (var field in nested.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    if (field.IsLiteral && !field.IsInitOnly && field.GetRawConstantValue() is string val)
                        yield return val;
                }
            }
        }

        public static IEnumerable<string> GetAll()
        {
            return GetTenantPermissions().Concat(GetPlatformPermissions());
        }
    }
}

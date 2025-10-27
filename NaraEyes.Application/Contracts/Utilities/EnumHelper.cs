using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Utilities
{
    public static class EnumHelper
    {
        public static string GetEnumDisplayName(Enum value)
        {
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());

            DisplayAttribute[] attributes = (DisplayAttribute[])fieldInfo.GetCustomAttributes(
                typeof(DisplayAttribute), false);

            if (attributes[0].ResourceType != null)
                return LookupResource(attributes[0].ResourceType, attributes[0].Name);


            if (attributes != null && attributes.Length > 0)
                return attributes[0].Name;
            else
                return value.ToString();

        }

        private static string LookupResource(Type resourceManagerProvider, string resourceKey)
        {
            foreach (PropertyInfo staticProperty in resourceManagerProvider.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (staticProperty.PropertyType == typeof(System.Resources.ResourceManager))
                {
                    System.Resources.ResourceManager resourceManager = (System.Resources.ResourceManager)staticProperty.GetValue(null, null);
                    return resourceManager.GetString(resourceKey);
                }
            }

            return resourceKey; // Fallback with the key name
        }

    }
}

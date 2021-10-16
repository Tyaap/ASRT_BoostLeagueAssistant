using System;
using System.ComponentModel;

namespace ASRT_BoostLeagueAssistant
{
    public static class Extensions
    {
        public static string GetDescription(this object enumerationValue)
        {
            var type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException($"{nameof(enumerationValue)} must be of Enum type", nameof(enumerationValue));
            }

            var memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo.Length > 0)
            {
                var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            return enumerationValue.ToString();
        }

        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException("Cannot get description for non-enum type " + type);
            var fields = type.GetFields();
            int fieldCount = fields.Length;
            for (int i = 0; i < fieldCount; i++)
            {
                var field = fields[i];
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException("Description \"" + description + "\" not found for enum of type " + type, nameof(description));
        }

        public static string GetDescriptionList<T>()
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            string s = "";
            var fields = type.GetFields();
            int fieldCount = fields.Length;
            for (int i = 0; i < fieldCount; i++)
            {
                var field = fields[i];
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    s += attribute.Description;
                    if (i != fieldCount - 1)
                    {
                        s += "\n";
                    }
                }
            }
            return s;
        }

        public static float ToFloat(this int hex)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(hex), 0);
        }

        public static int ToHex(this float f)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
        }
    }
}
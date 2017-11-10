namespace NCalc.Extension
{
    using System.Reflection;

    using System;

    public static class TypeExtension
    {
        public static TypeCode GetTypeCode(this Type type)
        {
            if (type.IsAssignableFrom(typeof(bool)))
            {
                return TypeCode.Boolean;
            }

            if (type.IsAssignableFrom(typeof(DateTime)))
            {
                return TypeCode.DateTime;
            }

            if (type.IsAssignableFrom(typeof(decimal)))
            {
                return TypeCode.Decimal;
            }

            if (type.IsAssignableFrom(typeof(double)))
            {
                return TypeCode.Double;
            }

            if (type.IsAssignableFrom(typeof(Single)))
            {
                return TypeCode.Single;
            }

            if (type.IsAssignableFrom(typeof(Byte)))
            {
                return TypeCode.Byte;
            }

            if (type.IsAssignableFrom(typeof(SByte)))
            {
                return TypeCode.SByte;
            }

            if (type.IsAssignableFrom(typeof(Int16)))
            {
                return TypeCode.Int16;
            }

            if (type.IsAssignableFrom(typeof(Int32)))
            {
                return TypeCode.Int32;
            }

            if (type.IsAssignableFrom(typeof(Int64)))
            {
                return TypeCode.Int64;
            }

            if (type.IsAssignableFrom(typeof(UInt16)))
            {
                return TypeCode.UInt16;
            }

            if (type.IsAssignableFrom(typeof(UInt32)))
            {
                return TypeCode.UInt32;
            }

            if (type.IsAssignableFrom(typeof(UInt64)))
            {
                return TypeCode.UInt64;
            }

            if (type.IsAssignableFrom(typeof(char)))
            {
                return TypeCode.Char;
            }

            if (type.IsAssignableFrom(typeof(Byte)))
            {
                return TypeCode.Byte;
            }

            if (type.IsAssignableFrom(typeof(string)))
            {
                return TypeCode.String;
            }

            throw new Exception("TypeCode not found");
        }
    }
}
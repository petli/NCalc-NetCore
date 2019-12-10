using System;
using System.Threading.Tasks;

namespace NCalc.Domain
{
    public class ValueExpression : LogicalExpression
    {
        public ValueExpression(object value, ValueType type)
        {
            Value = value;
            Type = type;
        }

        public ValueExpression(object value)
        {
            if (value.GetType().IsAssignableFrom(typeof(bool)))
            {
                Type = ValueType.Boolean;
            }

            if (value.GetType().IsAssignableFrom(typeof(DateTime)))
            {
                Type = ValueType.DateTime;
            }

            if (value.GetType().IsAssignableFrom(typeof(decimal)) ||
                value.GetType().IsAssignableFrom(typeof(double)) ||
                value.GetType().IsAssignableFrom(typeof(Single)))
            {
                Type = ValueType.Float;
            }

            if (value.GetType().IsAssignableFrom(typeof(Byte)) ||
                value.GetType().IsAssignableFrom(typeof(SByte)) ||
                value.GetType().IsAssignableFrom(typeof(Int16)) ||
                value.GetType().IsAssignableFrom(typeof(Int32)) ||
                value.GetType().IsAssignableFrom(typeof(Int64)) ||
                value.GetType().IsAssignableFrom(typeof(UInt16)) ||
                value.GetType().IsAssignableFrom(typeof(UInt32)) ||
                value.GetType().IsAssignableFrom(typeof(UInt64)) ||
                value.GetType().IsAssignableFrom(typeof(int)) 

                )
            {
                Type = ValueType.Integer;
            }

            if (value.GetType().IsAssignableFrom(typeof(string)))
            {
                Type = ValueType.String;
            }
            Value = value;
        }

        public ValueExpression(string value)
        {
            Value = value;
            Type = ValueType.String;
        }

        public ValueExpression(int value)
        {
            Value = value;
            Type = ValueType.Integer;
        }

        public ValueExpression(float value)
        {
            Value = value;
            Type = ValueType.Float;
        }

        public ValueExpression(DateTime value)
        {
            Value = value;
            Type = ValueType.DateTime;
        }

        public ValueExpression(bool value)
        {
            Value = value;
            Type = ValueType.Boolean;
        }

        public object Value { get; set; }
        public ValueType Type { get; set; }

        public override void Accept(LogicalExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override async Task AcceptAsync(IAsyncLogicalExpressionVisitor visitor)
        {
            await visitor.VisitAsync(this);
        }
    }

    public enum ValueType
    {
        Integer,
        String,
        DateTime,
        Float,
        Boolean
    }
}
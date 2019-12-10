using System.Threading.Tasks;

namespace NCalc.Domain
{
    public class Identifier : LogicalExpression
    {
        public Identifier(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public override void Accept(LogicalExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override async Task AcceptAsync(IAsyncLogicalExpressionVisitor visitor)
        {
            await visitor.VisitAsync(this);
        }
    }
}
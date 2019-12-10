using System.Threading.Tasks;

namespace NCalc.Domain
{
    public interface IAsyncLogicalExpressionVisitor
    {
        Task VisitAsync(LogicalExpression expression);
        Task VisitAsync(TernaryExpression expression);
        Task VisitAsync(BinaryExpression expression);
        Task VisitAsync(UnaryExpression expression);
        Task VisitAsync(ValueExpression expression);
        Task VisitAsync(Function function);
        Task VisitAsync(Identifier function);
    }
}
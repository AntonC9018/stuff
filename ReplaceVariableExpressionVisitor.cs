using System.Linq.Expressions;

namespace project;

public sealed class ReplaceVariableExpressionVisitor : ExpressionVisitor
{
    private static ThreadLocal<ReplaceVariableExpressionVisitor?> _ThreadInstance = new();
 
    public static ReplaceVariableExpressionVisitor GetInstance(
        Expression replacement, ParameterExpression parameter)
    {
        var visitor = _ThreadInstance.Value;
        if (visitor is null)
        {
            visitor = new(replacement, parameter);
            _ThreadInstance.Value = visitor;
        }
        else
        {
            visitor.Replacement = replacement;
            visitor.Parameter = parameter;
        }
        return visitor;
    } 
     
    public Expression Replacement { get; set; }
    public ParameterExpression Parameter { get; set; }
     
    private ReplaceVariableExpressionVisitor(
        Expression replacement,
        ParameterExpression parameter)
    {
        Replacement = replacement;
        Parameter = parameter;
    }
 
    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == Parameter)
            return Replacement;
 
        return base.VisitParameter(node);
    }
 
    public static Expression ReplaceParameterAndGetBody(LambdaExpression lambda, Expression parameterReplacement)
    {
        var visitor = GetInstance(parameterReplacement, lambda.Parameters[0]);
        return visitor.Visit(lambda.Body);
    }
}
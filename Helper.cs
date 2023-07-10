using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Flowqe.Backend.API.GraphQL.FinancesStatistics;

namespace project;

static file class Helper
{
    public static IQueryable<IGrouping<GroupingKey, Expense>> ApplyGrouping(
        this IQueryable<Expense> query,
        GroupingModel grouping)
    {
        // TODO: maybe cache all combinations?
        // TODO: cache this list.
        var list = new List<MemberAssignment>();
        list.Add(EntityGroupExpressions[(int) grouping.EntityGroup]);
        list.Add(DateScaleExpressions[(int) grouping.DateScale]);
        if (grouping.GroupByExpenseType)
            list.Add(ExpenseTypeAccessExpression);

        var groupingKey = Expression.MemberInit(
            Expression.New(typeof(GroupingKey)), list);

        var lambda = Expression.Lambda<Func<Expense, GroupingKey>>(
            groupingKey, ExpenseParameter);

        return query.GroupBy(lambda);
    }

    // This can be abstracted in a nice way (with constructors like FluentValidation)
    private static readonly ParameterExpression ExpenseParameter = Expression.Parameter(typeof(Expense), "e");
    private static readonly MemberAssignment[] EntityGroupExpressions;
    private static readonly MemberAssignment[] DateScaleExpressions;
    private static readonly MemberAssignment ExpenseTypeAccessExpression;

    static Helper()
    {
        MemberAssignment Create<T>(PropertyInfo p, Expression<Func<Expense, T>> expression)
        {
            var a = ReplaceVariableExpressionVisitor.ReplaceParameterAndGetBody(expression, ExpenseParameter);
            return Expression.Bind(p, a);
        }

        PropertyInfo GetProperty<T>(Expression<Func<GroupingKey, T>> a)
        {
            Expression t = a.Body;
            return (PropertyInfo)((MemberExpression) t).Member;
        }

        int GetEnumLength<T>()
        {
            return typeof(T).GetEnumValues().Length;
        }

        {
            EntityGroupExpressions = new MemberAssignment[GetEnumLength<EntityGroup>()];
            var propertyInfo = GetProperty(k => k.GroupingId);
            void Set(EntityGroup i, Expression<Func<Expense, string>> expression)
            {
                EntityGroupExpressions[(int) i] = Create(propertyInfo, expression);
            }

            Set(EntityGroup.Client, e => e.CompanyId.ToString());
            Set(EntityGroup.Project, e => 1.ToString());
            Set(EntityGroup.Company, e => e.CompanyId.ToString());
        }

        {
            DateScaleExpressions = new MemberAssignment[GetEnumLength<DateScale>()];
            var propertyInfo = GetProperty(k => k.DateFrom);
            void Set(DateScale i, Expression<Func<Expense, DateTime>> expression)
            {
                DateScaleExpressions[(int) i] = Create(propertyInfo, expression);
            }

            Set(DateScale.Day, e => new DateTime(e.Date.Year, e.Date.Month, e.Date.Day));
            Set(DateScale.Month, e => new DateTime(e.Date.Year, e.Date.Month, 1));
            Set(DateScale.Year, e => new DateTime(e.Date.Year, 1, 1));
        }

        {
            var propertyInfo = GetProperty(k => k.ExpenseTypeId);
            ExpenseTypeAccessExpression = Create(propertyInfo, e => e.ExpenseTypeId);
        }
    }
}


public class FreeList<T> where T : unmanaged
{
    private readonly T[] _array;
    private int _first;
    
    public FreeList(int capacity)
    {
        _array = new T[capacity];
        _first = 0;

        for (int i = 0; i < capacity - 1; i++)
        {
            ref int next = ref Unsafe.As<T, int>(ref _array[i]);
            next = i + 1;
        }

        {
            int i = capacity - 1;
            ref int next = ref Unsafe.As<T, int>(ref _array[i]);
            next = -1;
        }
    }

    public ref T this[int index] => ref _array[index];

    public int Allocate()
    {
        ref int firstElement = ref Unsafe.As<T, int>(ref _array[_first]);
        if (firstElement == -1)
            throw new InvalidOperationException("Out of memory");
        if (firstElement >= _array.Length)
            throw new InvalidOperationException("Corrupted memory");
        
        int i = _first;
        _first = firstElement;
        return i;
    } 
    
    public void Free(int index)
    {
        ref int freedElement = ref Unsafe.As<T, int>(ref _array[index]);
        freedElement = _first;
        _first = index;
    }
}
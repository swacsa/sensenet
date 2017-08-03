using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Azure.Querying
{
    internal class SnQueryToAzureQueryVisitor : SnQueryToStringVisitor
    {
        private readonly IQueryContext _context;

        public SnQueryToAzureQueryVisitor(IQueryContext context)
        {
            _context = context;
        }

        public override BooleanClause VisitBooleanClause(BooleanClause clause)
        {
            Visit(clause.Predicate);

            return clause; //base.VisitBooleanClause(clause);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SenseNet.Search.Azure.Querying.Models;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Azure.Querying
{
    internal class SnQueryToAzureQueryVisitor : SnQueryToStringVisitor
    {
        private readonly IQueryContext _context;
        private AzureSearchParameters _query = new AzureSearchParameters();
        private StringBuilder _searchText = new StringBuilder();
        private StringBuilder _filter = new StringBuilder();

        public AzureSearchParameters Result
        {
            get
            {
                var query = new AzureSearchParameters();
                query.SearchText = _searchText.ToString();
                query.Filter = _filter.ToString();
                //query.IncludeTotalResultCount
                return query;
            }
        }

        public SnQueryToAzureQueryVisitor(IQueryContext context)
        {
            _context = context;
        }

        public override SnQueryPredicate VisitTextPredicate(TextPredicate textPredicate)
        {
            var value = (string) Escape(textPredicate.Value);
            if (textPredicate.FieldName.Substring(0, 1) != "_")
            {
                _searchText.Append($"{textPredicate.FieldName}:");
            }
            _searchText.Append(value);
            BoostTostring(_searchText, textPredicate.Boost);
            FuzzyToString(_searchText, textPredicate.FuzzyValue);

            return base.VisitTextPredicate(textPredicate);
        }

        private void FuzzyToString(StringBuilder builder, double? fuzzyValue)
        {
            if (fuzzyValue.HasValue && fuzzyValue != SnQuery.DefaultFuzzyValue)
            {
                builder.Append("~").Append(fuzzyValue.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public override SnQueryPredicate VisitRangePredicate(RangePredicate rangePredicate)
        {
            var min = rangePredicate.Min;
            var max = rangePredicate.Max;
            var minExclusive = rangePredicate.MinExclusive;
            var maxExclusive = rangePredicate.MaxExclusive;
            string op;

            if (max != null)
            {
                op = maxExclusive ? " lt " : " le ";
                _filter.Append(rangePredicate.FieldName).Append(op).Append("'").Append(max).Append("'");
            }
            if (min != null)
            {
                op = minExclusive ? " gt " : " ge ";
                if (max != null)
                {
                    _filter.Append(" ");
                }
                _filter.Append(rangePredicate.FieldName).Append(op).Append("'").Append(min).Append("'");
            }

            //BoostTostring(rangePredicate.Boost);

            return base.VisitRangePredicate(rangePredicate);
        }

        private void BoostTostring(StringBuilder builder,  double? boost)
        {
            if (boost.HasValue && boost != SnQuery.DefaultSimilarity)
            {
                builder.Append("^").Append(boost.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        //private void BoostTostring(double? boost)
        //{
        //    if (boost.HasValue && boost != SnQuery.DefaultSimilarity)
        //        _filter.Append("^").Append(boost.Value.ToString(CultureInfo.InvariantCulture));
        //}


        private int _booleanCount;
        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate clauseList)
        {
            if (_booleanCount++ > 0)
                _searchText.Append("(");
            var list = base.VisitLogicalClauses(clauseList.Clauses);
            if (--_booleanCount > 0)
                _searchText.Append(")");
            return clauseList;
        }

        public override List<LogicalClause> VisitLogicalClauses(List<LogicalClause> clauses)
        {
            // The list item cannot be rewritten because this class is sealed.
            if (clauses.Count > 0)
            {
                VisitLogicalClause(clauses[0]);
                for (var i = 1; i < clauses.Count; i++)
                {
                    _searchText.Append(" ");
                    VisitLogicalClause(clauses[i]);
                }
            }
            return clauses;
        }

        public override LogicalClause VisitLogicalClause(LogicalClause clause)
        {
            Visit(clause.Predicate);
            switch (clause.Occur)
            {
                case Occurence.Default:
                case Occurence.Must:
                    _searchText.Append("+").Append(clause.Predicate);
                    break;
                case Occurence.MustNot:
                    _searchText.Append("-").Append(clause.Predicate);
                    break;
                case Occurence.Should:
                    _searchText.Append(clause.Predicate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(clause.Occur), clause.Occur, null);

            }
            return clause; 
        }
    }
}
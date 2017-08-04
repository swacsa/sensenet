using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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
        private Regex _escaperRegex = new Regex("[+ - && || ! ( ) { } [ ] ^ \" ~ * ? : \\ /]");
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
            if (textPredicate.FieldName != "_Text")
            {
                _searchText.Append($"{textPredicate.FieldName.Replace("#", "")}:");
            }
            var value = Escape(textPredicate.Value);
            var phrase = value.WordCount() > 1;
            if (phrase)
            {
                _searchText.Append("\"");
            }
            _searchText.Append(value);
            if (phrase)
            {
                _searchText.Append("\"");
            }
            BoostTostring(_searchText, textPredicate.Boost);
            FuzzyToString(_searchText, phrase, textPredicate.FuzzyValue);

            return base.VisitTextPredicate(textPredicate);
        }

        private string Escape(string value)
        {
            if (_escaperRegex.IsMatch(value))
            {
                return $"\"{value}\"";
            }
            return value;
        }

        private void FuzzyToString(StringBuilder builder, bool phrase, double? fuzzyValue)
        {
            if (fuzzyValue.HasValue && (phrase || fuzzyValue != SnQuery.DefaultFuzzyValue))
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

            return base.VisitRangePredicate(rangePredicate);
        }

        private void BoostTostring(StringBuilder builder,  double? boost)
        {
            if (boost.HasValue && boost != SnQuery.DefaultSimilarity)
            {
                builder.Append("^").Append(boost.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private int _booleanCount;
        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate clauseList)
        {
            if (_booleanCount++ > 0)
                _searchText.Append("(");
            VisitLogicalClauses(clauseList.Clauses);
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
            switch (clause.Occur)
            {
                case Occurence.Default: break;
                case Occurence.Should: break;
                case Occurence.Must:
                    _searchText.Append("+");
                    break;
                case Occurence.MustNot:
                    _searchText.Append("-");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(clause.Occur), clause.Occur, null);
            }
            Visit(clause.Predicate);
            return clause;
        }
    }
}
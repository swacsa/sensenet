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
    internal class SnQueryToAzureQueryVisitor : SnQueryVisitor
    {
        private int _booleanCount;
        private AzureSearchParameters _azureParameters;
        private readonly IQueryContext _context;
        private AzureSearchParameters _query = new AzureSearchParameters();
        private StringBuilder _searchText = new StringBuilder();
        private StringBuilder _filter = new StringBuilder();
        private Regex _escaperRegex = new Regex("[+ - && || ! ( ) { } [ ] ^ \" ~ * ? : \\ /]");
        public AzureSearchParameters Result
        {
            get
            {
                _azureParameters.SearchText = _searchText.ToString();
                _azureParameters.Filter = _filter.ToString();
                return _azureParameters;
            }
        }

        public SnQueryToAzureQueryVisitor(IQueryContext context, AzureSearchParameters azureParameters)
        {
            _context = context;
            _azureParameters = azureParameters;
        }

        public override SnQueryPredicate VisitTextPredicate(SimplePredicate textPredicate)
        {
            if (textPredicate.FieldName != "_Text")
            {
                _searchText.Append($"{textPredicate.FieldName.Replace("#", "")}:");
            }
            var value = Escape(textPredicate.Value.StringValue);
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
            var isNumeric = IsNumeric(rangePredicate.FieldName);
            string op;

            if (max != null)
            {
                op = maxExclusive ? " lt " : " le ";
                _filter.Append(rangePredicate.FieldName).Append(op);
                if (isNumeric)
                {
                    _filter.Append(max);
                }
                else
                {
                    _filter.Append("'").Append(max).Append("'");
                }
            }
            if (min != null)
            {
                op = minExclusive ? " gt " : " ge ";
                if (max != null)
                {
                    _filter.Append(" and ");
                }
                _filter.Append(rangePredicate.FieldName).Append(op);
                if (isNumeric)
                {
                    _filter.Append(min);
                }
                else
                {
                    _filter.Append("'").Append(min).Append("'");
                }
            }

            return base.VisitRangePredicate(rangePredicate);
        }

        private bool IsNumeric(string fieldName)
        {
            IPerFieldIndexingInfo info;
            try
            {
                info = _context.GetPerFieldIndexingInfo(fieldName);
            }
            catch
            {
                return false;
            }
            if (info == null)
            {
                return false;
            }
            bool result;
            switch (info.FieldDataType.Name)
            {
                case "Int32":
                case "Int64":
                case "Single":
                case "Double":
                    result = true;
                    break;
                default:
                    result = false;
                    break;
            }
            return result;
        }

        private void BoostTostring(StringBuilder builder,  double? boost)
        {
            if (boost.HasValue && boost != SnQuery.DefaultSimilarity)
            {
                builder.Append("^").Append(boost.Value.ToString(CultureInfo.InvariantCulture));
            }
        }


        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate predicate)
        {
            if (_booleanCount++ > 0)
                _searchText.Append("(");
            VisitLogicalClauses(predicate.Clauses);
            if (--_booleanCount > 0)
                _searchText.Append(")");
            return predicate;
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
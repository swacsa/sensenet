using System.Collections.Generic;
using System.Globalization;
using SenseNet.Search;
using SenseNet.Search.Parser;
using SenseNet.Search.Tests.Implementations;
using Xunit;
using SenseNet.Search.Azure.Querying.Models;
using System.Linq;

namespace Sensenet.Search.Azure.Tests
{
    public class AzureSearcherTests
    {
        private string SortToString(IList<string >orderBy)
        {
            return string.Join(", ", orderBy);
        }

        public void AstTest()
        {
            AzureSearchParameters q;
            Test("value", "value");
            Test("VALUE", "VALUE");
            Test("Value", "Value");
            Test("Value1", "Value1");
            Test("-Value1", "-Value1");
            Test("+Value1", "+Value1");
            Test("Value1 -Value2 +Value3 Value4", "Value1 -Value2 +Value3 Value4");
            Test("Field1:Value1");
            q = Test("#Field1:Value1");
            Assert.Equal("Field1/any(v: v eq 'Value1')", q.Filter);
            Test("-Field1:Value1");
            Test("+Field1:Value1");
            Test("Field1:Value1 Field2:Value2 Field3:Value3");
            Test("F1:V1 -F2:V2 +F3:V3 F4:V4");
            Test("f1:v1 f2:v2");
            Test("f1:v1 f2:v2 (f3:v3 f4:v4 (f5:v5 f6:v6))");
            Test("f1:v1 (f2:v2 (f3:v3 f4:v4))");
            Test("aaa AND +bbb", "+aaa +bbb");
            Test("te?t", "te?t");
            Test("test*", "test*");
            //Test("te*t", "te*t");
            Test("roam~", "roam");
            Test("roam~" + SnQuery.DefaultFuzzyValue.ToString(CultureInfo.InvariantCulture), "roam");
            Test("roam~0.8", "roam~0.8");
            Test("\"jakarta apache\"~10");
            Test("mod_date:[20020101 TO 20030101]"); Assert.Equal("mod_date ge 20020101 and mod_date le 20030101", q.Filter);
            Test("title:{Aida TO Carmen}"); Assert.Equal("title gt Aida and title lt Carmen", q.Filter);
            Test("jakarta apache");
            Test("jakarta^4 apache");
            Test("\"jakarta apache\"^4 \"Apache Lucene\"");
            Test("\"jakarta apache\" jakarta");
            Test("\"jakarta apache\" OR jakarta", "\"jakarta apache\" jakarta");
            Test("\"jakarta apache\" AND \"Apache Lucene\"", "+\"jakarta apache\" +\"Apache Lucene\"");
            Test("+jakarta lucene", "+jakarta lucene");
            Test("\"jakarta apache\" NOT \"Apache Lucene\"", "\"jakarta apache\" -\"Apache Lucene\"");
            Test("NOT \"jakarta apache\"", "-\"jakarta apache\"");
            Test("\"jakarta apache\" -\"Apache Lucene\"");
            Test("(jakarta OR apache) AND website", "+(jakarta apache) +website");
            Test("title:(+return +\"pink panther\")");

            Test("F1:V1 && F2:V2", "+F1:V1 +F2:V2");
            Test("F1:V1 || F2:V2", "F1:V1 F2:V2");
            Test("F1:V1 && F2:<>V2", "+F1:V1 -F2:V2");
            Test("F1:V1 && !F2:V2", "+F1:V1 -F2:V2");

            Test("F1:V1 //asdf", "F1:V1");
            Test("+F1:V1 /*asdf*/ +F2:V2 /*qwer*/", "+F1:V1 +F2:V2");

            q = Test("F1:V1 .TOP:42", "F1:V1"); Assert.Equal(42, q.Top);
            q = Test("F1:V1 .SKIP:42", "F1:V1"); Assert.Equal(42, q.Skip);
            q = Test("F1:V1 .COUNTONLY", "F1:V1"); Assert.Equal(true, q.IncludeTotalResultCount && q.Top == 0);
            q = Test("F1:V1 .AUTOFILTERS:ON", "F1:V1"); Assert.True(q.EnableAutofilters);
            q = Test("F1:V1 .AUTOFILTERS:OFF", "F1:V1"); Assert.False(q.EnableAutofilters);
            q = Test("F1:V1 .LIFESPAN:ON", "F1:V1"); Assert.True(q.EnableLifespanFilter);
            q = Test("F1:V1 .LIFESPAN:OFF", "F1:V1"); Assert.False(q.EnableLifespanFilter);
            q = Test("F1:V1 .QUICK", "F1:V1");
            q = Test("F1:V1 .SELECT:Name", "F1:V1"); Assert.Equal("Name", q.Select[0]);
            // q.OrderBy.Count <= 32
            q = Test("F1:V1 .SORT:F1", "F1:V1"); Assert.Equal("F1", q.OrderBy[0]);
            q = Test("F1:V1 .REVERSESORT:F1", "F1:V1"); Assert.Equal("F1 desc", q.OrderBy[0]);
            q = Test("F1:V1 .SORT:F1 .SORT:F2", "F1:V1"); Assert.Equal("F1, F2", SortToString(q.OrderBy));
            q = Test("F1:V1 .SORT:F1 .REVERSESORT:F3 .SORT:F2", "F1:V1"); Assert.Equal("F1, F3 desc, F2", SortToString(q.OrderBy));

            q = Test("Name:<aaa"); Assert.Equal("Name lt 'aaa'", q.Filter);
            q = Test("Name:>aaa"); Assert.Equal("Name gt 'aaa'", q.Filter);
            q = Test("Name:<=aaa"); Assert.Equal("Name le 'aaa'", q.Filter);
            q = Test("Name:>=aaa"); Assert.Equal("Name ge 'aaa'", q.Filter);
            q = Test("Id:<1000"); Assert.Equal("Id lt 1000", q.Filter);
            q = Test("Id:>1000"); Assert.Equal("Id gt 1000", q.Filter);
            q = Test("Id:<=1000"); Assert.Equal("Id le 1000", q.Filter);
            q = Test("Id:>=1000"); Assert.Equal("Id ge 1000", q.Filter);
            q = Test("Value:<3.14"); Assert.Equal("Value lt 3.14", q.Filter);
            q = Test("Value:>3.14"); Assert.Equal("Value gt 3.14", q.Filter);
            q = Test("Value:<=3.14"); Assert.Equal("Value le 3.14", q.Filter);
            q = Test("Value:>=3.14"); Assert.Equal("Value ge 3.14", q.Filter);

        }

        private AzureSearchParameters Test(string queryText, string expected = null)
        {
            var queryContext = new TestQueryContext(QuerySettings.Default, 0, null);
            var parser = new CqlParser();

            var snQuery = parser.Parse(queryText, queryContext);

            var visitor = new SenseNet.Search.Azure.Querying.SnQueryToAzureQueryVisitor(queryContext);
            visitor.Visit(snQuery.QueryTree);
            var actualResult = visitor.Result;

            Assert.Equal(expected ?? queryText, actualResult.SearchText);
            return actualResult;
        }

    }
}
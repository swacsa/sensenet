using System.Collections.Generic;
using System.Globalization;
using SenseNet.Search;
using SenseNet.Search.Parser;
using SenseNet.Search.Tests.Implementations;
using Xunit;
using SenseNet.Search.Azure.Querying.Models;
using System.Linq;
using SenseNet.Search.Azure.Querying;

namespace Sensenet.Search.Azure.Tests
{
    public class AzureQueryCompilerTests
    {

        [Fact]
        public void AzureCompilerTest()
        {
            AzureSearchParameters q;
            Test("value");
            Test("VALUE");
            Test("Value");
            Test("Value1");
            Test("-Value1");
            Test("+Value1");
            Test("Value1 -Value2 +Value3 Value4");
            Test("Field1:Value1");
            Test("#Field1:Value1", "Field1:Value1");
            Test("-Field1:Value1");
            Test("+Field1:Value1");
            Test("Field1:Value1 Field2:Value2 Field3:Value3");
            Test("F1:V1 -F2:V2 +F3:V3 F4:V4");
            Test("f1:v1 f2:v2");
            Test("f1:v1 f2:v2 (f3:v3 f4:v4 (f5:v5 f6:v6))");
            Test("f1:v1 (f2:v2 (f3:v3 f4:v4))");
            Test("aaa AND +bbb", "+aaa +bbb");
            Test("te?t");
            Test("test*");
            Test("te*t");
            Test("roam~", "roam");
            Test("roam~" + SnQuery.DefaultFuzzyValue.ToString(CultureInfo.InvariantCulture), "roam");
            Test("roam~0.8");
            Test("\"jakarta apache\"~10");
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
            Test("title:(+return +\"pink panther\")", "+title:return +title:\"pink panther\"");

            Test("F1:V1 && F2:V2", "+F1:V1 +F2:V2");
            Test("F1:V1 || F2:V2", "F1:V1 F2:V2");
            Test("F1:V1 && F2:<>V2", "+F1:V1 -F2:V2");
            Test("F1:V1 && !F2:V2", "+F1:V1 -F2:V2");

            Test("F1:V1 //asdf", "F1:V1");
            Test("+F1:V1 /*asdf*/ +F2:V2 /*qwer*/", "+F1:V1 +F2:V2");

            RangeTest("mod_date:[20020101 TO 20030101]", "mod_date le '20030101' and mod_date ge '20020101'");
            RangeTest("title:{Aida TO Carmen}", "title lt 'Carmen' and title gt 'Aida'");
            RangeTest("title:[Aida TO Carmen}", "title lt 'Carmen' and title ge 'Aida'");
            RangeTest("title:{Aida TO Carmen]", "title le 'Carmen' and title gt 'Aida'");
            RangeTest("Name:<aaa", "Name lt 'aaa'");
            RangeTest("Name:>aaa", "Name gt 'aaa'");
            RangeTest("Name:<=aaa", "Name le 'aaa'");
            RangeTest("Name:>=aaa", "Name ge 'aaa'");
            RangeTest("Id:<1000", "Id lt 1000");
            RangeTest("Id:>1000", "Id gt 1000");
            RangeTest("Id:<=1000", "Id le 1000");
            RangeTest("Id:>=1000", "Id ge 1000");
            RangeTest("Value:<3.14", "Value lt 3.14");
            RangeTest("Value:>3.14", "Value gt 3.14");
            RangeTest("Value:<=3.14", "Value le 3.14");
            RangeTest("Value:>=3.14", "Value ge 3.14");

            q = Test("F1:V1 .TOP:42", "F1:V1"); Assert.Equal(42, q.Top);
            q = Test("F1:V1 .SKIP:42", "F1:V1"); Assert.Equal(42, q.Skip);
            q = Test("F1:V1 .COUNTONLY", "F1:V1"); Assert.Equal(true, q.IncludeTotalResultCount && q.Top == 0);
            q = Test("F1:V1 .AUTOFILTERS:ON", "F1:V1"); Assert.True(q.EnableAutofilters);
            q = Test("F1:V1 .AUTOFILTERS:OFF", "F1:V1"); Assert.False(q.EnableAutofilters);
            q = Test("F1:V1 .LIFESPAN:ON", "F1:V1"); Assert.True(q.EnableLifespanFilter);
            q = Test("F1:V1 .LIFESPAN:OFF", "F1:V1"); Assert.False(q.EnableLifespanFilter);
            q = Test("F1:V1 .QUICK", "F1:V1");
            q = Test("F1:V1 .SELECT:Name,Id", "F1:V1"); Assert.Equal("Name", q.Select[0]); Assert.Equal("Id", q.Select[1]);
            q = Test("F1:V1 .SORT:F1", "F1:V1"); Assert.Equal("F1", q.OrderBy[0]);
            q = Test("F1:V1 .REVERSESORT:F1", "F1:V1"); Assert.Equal("F1 desc", q.OrderBy[0]);
            q = Test("F1:V1 .SORT:F1 .SORT:F2", "F1:V1"); Assert.Equal("F1", q.OrderBy[0]); Assert.Equal("F2", q.OrderBy[1]);
            q = Test("F1:V1 .SORT:F1 .REVERSESORT:F3 .SORT:F2", "F1:V1"); Assert.Equal("F1", q.OrderBy[0]); Assert.Equal("F3 desc", q.OrderBy[1]); Assert.Equal("F2", q.OrderBy[2]);


        }

        private AzureSearchParameters Test(string queryText, string expected = null)
        {

            IDictionary<string, IPerFieldIndexingInfo> indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>();
            indexingInfo.Add("Id", new TestPerfieldIndexingInfoInt());
            indexingInfo.Add("Name", new TestPerfieldIndexingInfoString());
            indexingInfo.Add("Value", new TestPerfieldIndexingInfoSingle());
            IQueryContext queryContext = new QueryContext(QuerySettings.Default, 0, indexingInfo);
            var parser = new CqlParser();
            var snQuery = parser.Parse(queryText, queryContext);
            var compiler = new AzureQueryCompiler();

            var actualResult = compiler.Compile(snQuery, queryContext);

            Assert.Equal(expected ?? queryText, actualResult.SearchText);
            return actualResult;
        }

        private AzureSearchParameters RangeTest(string queryText, string expected = null)
        {
            IDictionary<string, IPerFieldIndexingInfo> indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>();
            indexingInfo.Add("Id", new TestPerfieldIndexingInfoInt());
            indexingInfo.Add("Name", new TestPerfieldIndexingInfoString());
            indexingInfo.Add("Value", new TestPerfieldIndexingInfoSingle());
            IQueryContext queryContext = new QueryContext(QuerySettings.Default, 0, indexingInfo);
            var parser = new CqlParser();
            var snQuery = parser.Parse(queryText, queryContext);
            var compiler = new AzureQueryCompiler();

            var actualResult = compiler.Compile(snQuery, queryContext);

            Assert.Equal(expected ?? queryText, actualResult.Filter);
            return actualResult;
        }


    }
}
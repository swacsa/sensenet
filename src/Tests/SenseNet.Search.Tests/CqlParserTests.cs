using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;
using SenseNet.Search.Tests.Implementations;
using SnQueryToStringVisitor = SenseNet.Search.Tests.Implementations.SnQueryToStringVisitor;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class CqlParserTests
    {
        [TestMethod]
        public void Search_Parser_AstToString_FromOriginalLuceneQueryParserSyntax()
        {
            Test("value", "_Text:value");
            Test("VALUE", "_Text:VALUE");
            Test("Value", "_Text:Value");
            Test("Value1", "_Text:Value1");
            Test("-Value1", "-_Text:Value1");
            Test("+Value1", "+_Text:Value1");
            Test("Value1 -Value2 +Value3 Value4", "_Text:Value1 -_Text:Value2 +_Text:Value3 _Text:Value4");
            Test("Field1:Value1");
            Test("#Field1:Value1");
            Test("-Field1:Value1");
            Test("+Field1:Value1");
            Test("Field1:Value1 Field2:Value2 Field3:Value3");
            Test("F1:V1 -F2:V2 +F3:V3 F4:V4");
            Test("f1:v1 f2:v2");
            Test("f1:v1 f2:v2 (f3:v3 f4:v4 (f5:v5 f6:v6))");
            Test("f1:v1 (f2:v2 (f3:v3 f4:v4))");
            Test("aaa AND +bbb", "+_Text:aaa +_Text:bbb");
            Test("te?t", "_Text:te?t");
            Test("test*", "_Text:test*");
            Test("te*t", "_Text:te*t");
            Test("roam~", "_Text:roam");
            Test("roam~" + SnQuery.DefaultFuzzyValue.ToString(CultureInfo.InvariantCulture), "_Text:roam");
            Test("roam~0.8", "_Text:roam~0.8");
            Test("\"jakarta apache\"~10", "_Text:'jakarta apache'~10");
            Test("mod_date:[20020101 TO 20030101]");
            Test("title:{Aida TO Carmen}");
            Test("jakarta apache", "_Text:jakarta _Text:apache");
            Test("jakarta^4 apache", "_Text:jakarta^4 _Text:apache");
            Test("\"jakarta apache\"^4 \"Apache Lucene\"", "_Text:'jakarta apache'^4 _Text:'Apache Lucene'");
            Test("\"jakarta apache\" jakarta", "_Text:'jakarta apache' _Text:jakarta");
            Test("\"jakarta apache\" OR jakarta", "_Text:'jakarta apache' _Text:jakarta");
            Test("\"jakarta apache\" AND \"Apache Lucene\"", "+_Text:'jakarta apache' +_Text:'Apache Lucene'");
            Test("+jakarta lucene", "+_Text:jakarta _Text:lucene");
            Test("\"jakarta apache\" NOT \"Apache Lucene\"", "_Text:'jakarta apache' -_Text:'Apache Lucene'");
            Test("NOT \"jakarta apache\"", "-_Text:'jakarta apache'");
            Test("\"jakarta apache\" -\"Apache Lucene\"", "_Text:'jakarta apache' -_Text:'Apache Lucene'");
            Test("(jakarta OR apache) AND website", "+(_Text:jakarta _Text:apache) +_Text:website");
            Test("title:(+return +\"pink panther\")", "+title:return +title:'pink panther'");
        }
        [TestMethod]
        public void Search_Parser_AstToString_AdditionalTests()
        {
            Test("42value", "_Text:42value");
            Test("42v?lue", "_Text:42v?lue");
            Test("42valu*", "_Text:42valu*");
            Test("Name:42aAa", "Name:42aAa");
            Test("Name:42a?a");
            Test("Name:42aa*");
            Test("(Name:aaa Id:2)", "Name:aaa Id:2"); // unnecessary parenthesis
            TestError("Name:\"aaa", "Unclosed string");
        }
        [TestMethod]
        public void Search_Parser_AstToString_AdditionalTests2()
        {
            Test("Name:[a TO c}");
            Test("Name:{a TO c]");
            Test("Index:[2 TO 3}");
            Test("Index:{2 TO 3]");

            TestError("Name:a TO c}", "Unexpected 'TO'");
            TestError("Name:a TO c]", "Unexpected 'TO'");
            TestError("Index:2 TO 3}", "Unexpected 'TO'");
            TestError("Index:2 TO 3]", "Unexpected 'TO'");

            TestError("Name:[a TO c", "Unterminated Range expression");
            TestError("Name:{a TO c", "Unterminated Range expression");
            TestError("Index:[2 TO 3", "Unterminated Range expression");
            TestError("Index:{2 TO 3", "Unterminated Range expression");
        }

        [TestMethod]
        public void Search_Parser_AstToString_MultiBool()
        {
            BoolTest("a (b c)");
            BoolTest("a (b +c)");
            BoolTest("a (b -c)");
            BoolTest("a (+b c)");
            BoolTest("a (+b +c)");
            BoolTest("a (+b -c)");
            BoolTest("a (-b c)");
            BoolTest("a (-b +c)");
            BoolTest("a (-b -c)");

            BoolTest("a +(b c)");
            BoolTest("a +(b +c)");
            BoolTest("a +(b -c)");
            BoolTest("a +(+b c)");
            BoolTest("a +(+b +c)");
            BoolTest("a +(+b -c)");
            BoolTest("a +(-b c)");
            BoolTest("a +(-b +c)");
            BoolTest("a +(-b -c)");

            BoolTest("a -(b c)");
            BoolTest("a -(b +c)");
            BoolTest("a -(b -c)");
            BoolTest("a -(+b c)");
            BoolTest("a -(+b +c)");
            BoolTest("a -(+b -c)");
            BoolTest("a -(-b c)");
            BoolTest("a -(-b +c)");
            BoolTest("a -(-b -c)");

            BoolTest("+a (b c)");
            BoolTest("+a (b +c)");
            BoolTest("+a (b -c)");
            BoolTest("+a (+b c)");
            BoolTest("+a (+b +c)");
            BoolTest("+a (+b -c)");
            BoolTest("+a (-b c)");
            BoolTest("+a (-b +c)");
            BoolTest("+a (-b -c)");

            BoolTest("+a +(b c)");
            BoolTest("+a +(b +c)");
            BoolTest("+a +(b -c)");
            BoolTest("+a +(+b c)");
            BoolTest("+a +(+b +c)");
            BoolTest("+a +(+b -c)");
            BoolTest("+a +(-b c)");
            BoolTest("+a +(-b +c)");
            BoolTest("+a +(-b -c)");

            BoolTest("+a -(b c)");
            BoolTest("+a -(b +c)");
            BoolTest("+a -(b -c)");
            BoolTest("+a -(+b c)");
            BoolTest("+a -(+b +c)");
            BoolTest("+a -(+b -c)");
            BoolTest("+a -(-b c)");
            BoolTest("+a -(-b +c)");
            BoolTest("+a -(-b -c)");

            BoolTest("-a (b c)");
            BoolTest("-a (b +c)");
            BoolTest("-a (b -c)");
            BoolTest("-a (+b c)");
            BoolTest("-a (+b +c)");
            BoolTest("-a (+b -c)");
            BoolTest("-a (-b c)");
            BoolTest("-a (-b +c)");
            BoolTest("-a (-b -c)");

            BoolTest("-a +(b c)");
            BoolTest("-a +(b +c)");
            BoolTest("-a +(b -c)");
            BoolTest("-a +(+b c)");
            BoolTest("-a +(+b +c)");
            BoolTest("-a +(+b -c)");
            BoolTest("-a +(-b c)");
            BoolTest("-a +(-b +c)");
            BoolTest("-a +(-b -c)");

            BoolTest("-a -(b c)");
            BoolTest("-a -(b +c)");
            BoolTest("-a -(b -c)");
            BoolTest("-a -(+b c)");
            BoolTest("-a -(+b +c)");
            BoolTest("-a -(+b -c)");
            BoolTest("-a -(-b c)");
            BoolTest("-a -(-b +c)");
            BoolTest("-a -(-b -c)");

        }
        [TestMethod]
        public void Search_Parser_AstToString_MultiBool_AndOr()
        {
            BoolTest("a OR (b OR c)"                  , "a (b c)");
            BoolTest("a OR (b +c)"                    , "a (b +c)"   );
            BoolTest("a OR (b -c)"                    , "a (b -c)"   );
            BoolTest("a OR (+b c)"                    , "a (+b c)"   );
            BoolTest("a OR (b AND c)"                 , "a (+b +c)"  );
            BoolTest("a OR (+b -c)"                   , "a (+b -c)"  );
            BoolTest("a OR (-b c)"                    , "a (-b c)"   );
            BoolTest("a OR (-b +c)"                   , "a (-b +c)"  );
            BoolTest("a OR (NOT b OR NOT c)"          , "a (-b -c)"  );

            BoolTest("a +(b OR c)"                    , "a +(b c)"   );
            BoolTest("a +(b AND c)"                   , "a +(+b +c)" );
            BoolTest("a +(NOT b OR NOT c)"            , "a +(-b -c)" );

            BoolTest("a -(b OR c)"                    , "a -(b c)"   );
            BoolTest("a -(b AND c)"                   , "a -(+b +c)" );
            BoolTest("a -(NOT b OR NOT c)"            , "a -(-b -c)" );

            BoolTest("+a (b OR c)"                    , "+a (b c)"   );
            BoolTest("+a (b AND c)"                   , "+a (+b +c)" );
            BoolTest("+a (NOT b OR NOT c)"            , "+a (-b -c)" );

            BoolTest("a AND (b OR c)"                 , "+a +(b c)"  );
            BoolTest("a AND (b +c)"                   , "+a +(b +c)" );
            BoolTest("a AND (b -c)"                   , "+a +(b -c)" );
            BoolTest("a AND (+b c)"                   , "+a +(+b c)" );
            BoolTest("a AND (b AND c)"                , "+a +(+b +c)");
            BoolTest("a AND (+b -c)"                  , "+a +(+b -c)");
            BoolTest("a AND (-b c)"                   , "+a +(-b c)" );
            BoolTest("a AND (-b +c)"                  , "+a +(-b +c)");
            BoolTest("a AND (NOT b OR NOT c)"         , "+a +(-b -c)");

            BoolTest("+a -(b OR c)"                   , "+a -(b c)"  );
            BoolTest("+a -(b AND c)"                  , "+a -(+b +c)");
            BoolTest("+a -(NOT b OR NOT c)"           , "+a -(-b -c)");

            BoolTest("-a (b OR c)"                    , "-a (b c)"   );
            BoolTest("-a (b AND c)"                   , "-a (+b +c)" );
            BoolTest("-a (NOT b OR NOT c)"            , "-a (-b -c)" );

            BoolTest("-a +(b OR c)"                   , "-a +(b c)"  );
            BoolTest("-a +(b AND c)"                  , "-a +(+b +c)");
            BoolTest("-a +(NOT b OR NOT c)"           , "-a +(-b -c)");

            BoolTest("NOT a AND NOT (b OR c)"         , "-a -(b c)"  );
            BoolTest("NOT a AND NOT (b +c)"           , "-a -(b +c)" );
            BoolTest("NOT a AND NOT (b -c)"           , "-a -(b -c)" );
            BoolTest("NOT a AND NOT (+b c)"           , "-a -(+b c)" );
            BoolTest("NOT a AND NOT (b AND c)"        , "-a -(+b +c)");
            BoolTest("NOT a AND NOT (+b -c)"          , "-a -(+b -c)");
            BoolTest("NOT a AND NOT (-b c)"           , "-a -(-b c)" );
            BoolTest("NOT a AND NOT (-b +c)"          , "-a -(-b +c)");
            BoolTest("NOT a AND NOT (NOT b OR NOT c)" , "-a -(-b -c)");
        }
        [TestMethod]
        public void Search_Parser_AstToString_MultiBool_AndOrNot()
        {
            BoolTest("a AND NOT b", "+a -b");
            BoolTest("a OR NOT b", "a -b");
            BoolTest("NOT a AND NOT b", "-a -b");
            BoolTest("NOT a OR NOT b", "-a -b");
        }

        [TestMethod]
        public void Search_Parser_AstToString_MultiBool_Mix()
        {
            BoolTest("a AND b",   "+a +b");
            BoolTest("a AND +b",  "+a +b");
            BoolTest("a AND -b",  "+a -b");
            BoolTest("+a AND b",  "+a +b");
            BoolTest("+a AND +b", "+a +b");
            BoolTest("+a AND -b", "+a -b");
            BoolTest("-a AND b",  "-a +b");
            BoolTest("-a AND +b", "-a +b");
            BoolTest("-a AND -b", "-a -b");

            BoolTest("a OR b",   "a b");
            BoolTest("a OR +b",  "a +b");
            BoolTest("a OR -b",  "a -b");
            BoolTest("+a OR b",  "+a b");
            BoolTest("+a OR +b", "+a +b");
            BoolTest("+a OR -b", "+a -b");
            BoolTest("-a OR b",  "-a b");
            BoolTest("-a OR +b", "-a +b");
            BoolTest("-a OR -b", "-a -b");
        }

        [TestMethod]
        public void Search_Parser_AstToString_EmptyQueries()
        {
            var empty = SnQuery.EmptyText;
            Test($"+(+F1:{empty} +F2:aaa*) +F3:bbb", "+(+F2:aaa*) +F3:bbb");
            Test($"+(+F1:{empty} +(F2:V2 F3:V3)) +F3:bbb", "+(+(F2:V2 F3:V3)) +F3:bbb");
            Test($"+(+F1:{empty} +F2:{empty}) +F3:bbb", "+F3:bbb");

            Test($"F1:[{empty} TO max]", "F1:<=max");
            Test($"F1:[min TO {empty}]", "F1:>=min");
            Test($"F1:[{empty} TO ]", "");
            Test($"F1:[ TO {empty}]", "");
            Test($"F1:[\"{empty}\" TO max]", "F1:<=max");
            Test($"F1:[min TO \"{empty}\"]", "F1:>=min");

            TestError($"F1:[{empty} TO {empty}]", "Invalid range");
        }

        [TestMethod]
        public void Search_Parser_AstToString_PredicateTypes()
        {
            SnQuery q;
            q = Test("Name:aaa"); Assert.AreEqual(typeof(TextPredicate), q.QueryTree.GetType());
            q = Test("Id:1000"); Assert.AreEqual(typeof(TextPredicate), q.QueryTree.GetType());
            q = Test("Value:3.14"); Assert.AreEqual(typeof(TextPredicate), q.QueryTree.GetType());
        }

        [TestMethod]
        public void Search_Parser_AstToString_CqlExtension_Ranges()
        {
            SnQuery q;
            q = Test("Name:<aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Name:>aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Name:<=aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Name:>=aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:<1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:>1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:<=1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:>=1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:<3.14");  Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:>3.14");  Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:<=3.14"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:>=3.14"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
        }
        [TestMethod]
        public void Search_Parser_AstToString_CqlExtension_SpecialChars()
        {
            Test("F1:(V1 OR V2)", "F1:V1 F1:V2");
            Test("F1:(V1 AND V2)", "+F1:V1 +F1:V2");
            Test("F1:(+V1 +V2 -V3)", "+F1:V1 +F1:V2 -F1:V3");
            Test("F1:(+V1 -(V2 V3))", "+F1:V1 -(F1:V2 F1:V3)");
            Test("F1:(+V1 !V2)", "+F1:V1 -F1:V2");

            Test("F1:V1 && F2:V2", "+F1:V1 +F2:V2");
            Test("F1:V1 || F2:V2", "F1:V1 F2:V2");
            Test("F1:V1 && F2:<>V2", "+F1:V1 -F2:V2");
            Test("F1:V1 && !F2:V2", "+F1:V1 -F2:V2");
            Test("F1:V1 && !(F2:V2 || F3:V3)", "+F1:V1 -(F2:V2 F3:V3)");

            Test("+Id:<1000\n+Name:a*", "+Id:<1000 +Name:a*");
            Test("Name:\\*", "Name:*");
            Test("Name:a<a");
            Test("Name:a>a");
            Test("Name<aaa", "_Text:Name<aaa");
            Test("Name>aaa", "_Text:Name>aaa");
            Test("Name:/root");
            Test("Number:42.15.78", "Number:42.15. _Text:78");

            Test("Aspect.Field1:aaa");
            Test("Aspect1.Field1:aaa");

            TestError("42.Field1:aaa", "Unexpected ':'");
            TestError("Name:a* |? Id:<1000", "Invalid operator: |");
            TestError("Name:a* &? Id:<1000", "Invalid operator: &");
            TestError("\"Name\":aaa", "Missing field name");
            TestError("'Name':aaa", "Missing field name");
            TestError("Name:\"aaa\\", "Unclosed string");
            TestError("Name:\"aaa\\\"", "Unclosed string");
            TestError("Name:<>:", "Unexpected Colon");
        }
        [TestMethod]
        public void Search_Parser_AstToString_CqlExtension_Comments()
        {
            Test("F1:V1 //asdf", "F1:V1");
            Test("+F1:V1 /*asdf*/ +F2:V2 /*qwer*/", "+F1:V1 +F2:V2");

            Test("Name:/* \n */aaa", "Name:aaa");
            Test("+Name:aaa //comment\n+Id:<42", "+Name:aaa +Id:<42");
            Test("+Name:aaa//comment\n+Id:<42", "+Name:aaa//comment +Id:<42");
            Test("+Name:\"aaa\"//comment\n+Id:<42", "+Name:aaa +Id:<42");
            Test("Name:aaa /*unterminatedblockcomment", "Name:aaa");
        }
        [TestMethod]
        public void Search_Parser_AstToString_CqlExtension_Controls()
        {
            // ".SELECT";
            // ".SKIP";
            // ".TOP";
            // ".SORT";
            // ".REVERSESORT";
            // ".AUTOFILTERS";
            // ".LIFESPAN";
            // ".COUNTONLY";
            // ".QUICK";

            var q = Test("F1:V1");
            Assert.AreEqual(int.MaxValue, q.Top);
            Assert.AreEqual(0, q.Skip);
            Assert.AreEqual(false, q.CountOnly);
            Assert.AreEqual(FilterStatus.Default, q.EnableAutofilters);
            Assert.AreEqual(FilterStatus.Default, q.EnableLifespanFilter);
            Assert.AreEqual(QueryExecutionMode.Default, q.QueryExecutionMode);
            Assert.AreEqual(null, q.Projection);
            Assert.AreEqual(0, q.Sort.Length);

            q = Test("F1:V1 .TOP:42", "F1:V1");
            Assert.AreEqual(42, q.Top);
            q = Test("F1:V1 .SKIP:42", "F1:V1");
            Assert.AreEqual(42, q.Skip);
            q = Test("F1:V1 .COUNTONLY", "F1:V1");
            Assert.AreEqual(true, q.CountOnly);
            q = Test("F1:V1 .AUTOFILTERS:ON", "F1:V1");
            Assert.AreEqual(FilterStatus.Enabled, q.EnableAutofilters);
            q = Test("F1:V1 .AUTOFILTERS:OFF", "F1:V1");
            Assert.AreEqual(FilterStatus.Disabled, q.EnableAutofilters);
            q = Test("F1:V1 .LIFESPAN:ON", "F1:V1");
            Assert.AreEqual(FilterStatus.Enabled, q.EnableLifespanFilter);
            q = Test("F1:V1 .LIFESPAN:OFF", "F1:V1");
            Assert.AreEqual(FilterStatus.Disabled, q.EnableLifespanFilter);
            q = Test("F1:V1 .QUICK", "F1:V1");
            Assert.AreEqual(QueryExecutionMode.Quick, q.QueryExecutionMode);
            q = Test("F1:V1 .SELECT:Name", "F1:V1");
            Assert.AreEqual("Name", q.Projection);

            q = Test("F1:V1 .SORT:F1", "F1:V1");
            Assert.AreEqual("F1 ASC", SortToString(q.Sort));
            q = Test("F1:V1 .REVERSESORT:F1", "F1:V1");
            Assert.AreEqual("F1 DESC", SortToString(q.Sort));
            q = Test("F1:V1 .SORT:F1 .SORT:F2", "F1:V1");
            Assert.AreEqual("F1 ASC, F2 ASC", SortToString(q.Sort));
            q = Test("F1:V1 .SORT:F1 .REVERSESORT:F3 .SORT:F2", "F1:V1");
            Assert.AreEqual("F1 ASC, F3 DESC, F2 ASC", SortToString(q.Sort));

            TestError("F1:V1 .UNKNOWNKEYWORD", "Unknown control keyword");
            TestError("F1:V1 .TOP", "Expected: Colon (':')");
            TestError("F1:V1 .TOP:", "Expected: Number");
            TestError("F1:V1 .TOP:aaa", "Expected: Number");
            TestError("F1:V1 .SKIP", "Expected: Colon (':')");
            TestError("F1:V1 .SKIP:", "Expected: Number");
            TestError("F1:V1 .SKIP:aaa", "Expected: Number");
            TestError("F1:V1 .COUNTONLY:", "Unexpected ':'");
            TestError("F1:V1 .COUNTONLY:aaa", "Unexpected ':'");
            TestError("F1:V1 .COUNTONLY:42", "Unexpected ':'");
            TestError("F1:V1 .COUNTONLY:ON", "Unexpected ':'");
            TestError("F1:V1 .AUTOFILTERS", "Expected: Colon (':')");
            TestError("F1:V1 .AUTOFILTERS:", "Expected: 'ON' or 'OFF'");
            TestError("F1:V1 .AUTOFILTERS:42", "Expected: 'ON' or 'OFF'");
            TestError("F1:V1 .LIFESPAN", "Expected: Colon (':')");
            TestError("F1:V1 .LIFESPAN:", "Expected: 'ON' or 'OFF'");
            TestError("F1:V1 .LIFESPAN:42", "Expected: 'ON' or 'OFF'");
            TestError("F1:V1 .QUICK:", "Unexpected ':'");
            TestError("F1:V1 .QUICK:aaa", "Unexpected ':'");
            TestError("F1:V1 .QUICK:42", "Unexpected ':'");
            TestError("F1:V1 .QUICK:ON", "Unexpected ':'");
            TestError("F1:V1 .SORT", "Expected: Colon (':')");
            TestError("F1:V1 .SORT:", "Expected: String");
            TestError("F1:V1 .SORT:42", "Expected: String");
            TestError("F1:V1 .SELECT", "Expected: Colon (':')");
            TestError("F1:V1 .SELECT:", "Expected: String");
            TestError("F1:V1 .SELECT:123", "Expected: String");
        }

        [TestMethod]
        public void Search_Parser_AstToString_CqlErrors()
        {
            TestError("", "Empty query is not allowed.");
            TestError("()", "Empty query is not allowed.");
            TestError("+(+(Id:1 Id:2) +Name:<b", "Missing ')'");
            TestError("Id:(1 2 3", "Expected: ')'");
            TestError("Password:asdf", "Cannot search by 'Password' field");
            TestError("PasswordHash:asdf", "Cannot search by 'PasswordHash' field");
            TestError("Id::1", "Unexpected ':'");
            TestError("Id:[10 to 15]", "Syntax error");
            TestError("Id:[10 TO 15", "Unterminated Range expression");
            TestError("Id:[ TO ]", "Invalid range");
            TestError("_Text:\"aaa bbb\"~", "Missing proximity value");
            TestError("Name:aaa~1.5", "Invalid fuzzy value");
            TestError("Name:aaa^x", "Syntax error");
            //UNDONE: Nullref exception in this test: Test("Name:()");
        }

        /* ============================================================================= */
        private SnQuery BoolTest(string queryText, string expected = null)
        {
            return Test(TransformToTerms(queryText), TransformToTerms(expected));
        }
        private string TransformToTerms(string text)
        {
            if (text == null)
                return null;

            for (var c = 'a'; c < 'e'; c++)
                text = text.Replace(c.ToString(), $"F{c}:{c}");

            return text;
        }

        private SnQuery Test(string queryText, string expected = null)
        {
            var queryContext = new TestQueryContext(QuerySettings.Default, 0, null);
            var parser = new CqlParser();

            var snQuery = parser.Parse(queryText, queryContext);

            var visitor = new SnQueryToStringVisitor();
            visitor.Visit(snQuery.QueryTree);
            var actualResult = visitor.Output;

            Assert.AreEqual(expected ?? queryText, actualResult);
            return snQuery;
        }

        private void TestError(string queryText, string expectedMessageSubstring)
        {
            var queryContext = new TestQueryContext(QuerySettings.Default, 0, null);
            var parser = new CqlParser();
            Exception thrownException = null;
            try
            {
                parser.Parse(queryText, queryContext);
            }
            catch (Exception e)
            {
                thrownException = e;
            }
            if (thrownException == null)
                Assert.Fail("Any exception wasn't thrown");
            if (expectedMessageSubstring != null && !thrownException.Message.Contains(expectedMessageSubstring))
                Assert.Fail($"Error message does not contain '{expectedMessageSubstring}'. Actual message: <{thrownException.Message}>");
        }

        private string SortToString(SortInfo[] sortInfo)
        {
            return string.Join(", ", sortInfo.Select(s => $"{s.FieldName} {(s.Reverse ? "DESC" : "ASC")}").ToArray());
        }

        [TestMethod]
        public void Search_Parser_AggregateSettingsTest()
        {
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                {"Id", new TestPerfieldIndexingInfoInt() }
            };
            // tuple values:
            // Item1: QuerySettings
            // Item2: query text postfix
            // Item3: expected Top
            // Item4: expected Skip
            // Item5: expected EnableAutofilters
            // Item6: expected EnableLifespanFilter
            // Item7: expected QueryExecutionMode
            var settings = new List<Tuple<QuerySettings, string, int, int, FilterStatus, FilterStatus, QueryExecutionMode>>
            {
                Tuple.Create(new QuerySettings(), " .TOP:0", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .TOP:5", 5, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 10}, "", 10, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 10}, " .TOP:0", 10, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 0}, " .TOP:10", 10, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 5}, " .TOP:10", 5, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 10}, " .TOP:5", 5, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .SKIP:0", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .SKIP:1", int.MaxValue, 1, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 0}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 0}, " .SKIP:1", int.MaxValue, 1, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 1}, " .SKIP:0", int.MaxValue, 1, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 10}, " .SKIP:5", int.MaxValue, 10, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 5}, " .SKIP:10", int.MaxValue, 5, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .AUTOFILTERS:ON", int.MaxValue, 0, FilterStatus.Enabled, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Default}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Enabled}, "", int.MaxValue, 0, FilterStatus.Enabled, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Disabled}, " .AUTOFILTERS:ON", int.MaxValue, 0, FilterStatus.Disabled, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .LIFESPAN:ON", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Enabled, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableLifespanFilter = FilterStatus.Default}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableLifespanFilter = FilterStatus.Enabled}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Enabled, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableLifespanFilter = FilterStatus.Disabled}, " .LIFESPAN:ON", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Disabled, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings() , " .QUICK", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Quick),
                Tuple.Create(new QuerySettings {QueryExecutionMode = QueryExecutionMode.Default}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {QueryExecutionMode = QueryExecutionMode.Quick}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Quick),
                Tuple.Create(new QuerySettings {QueryExecutionMode = QueryExecutionMode.Strict}, " .QUICK", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Strict),
                Tuple.Create(new QuerySettings {Sort = new List<SortInfo> {new SortInfo ("Id") } }, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings (), " .SORT:Id", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Sort = new List<SortInfo> {new SortInfo("Id") } }, " .SORT:Name", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .SORT:Name .TOP:0 .SORT:DisplayName", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default)
            };
            var expectedSortInfo = new List<IEnumerable<SortInfo>>();
            for (int i = 0; i < settings.Count - 4; i++)
            {
                expectedSortInfo.Add(null);
            }
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Id")});
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Id")});
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Id")});
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Name"), new SortInfo("DisplayName")});

            var parser = new CqlParser();
            var queryText = "+Id:<1000";
            foreach (var setting in settings)
            {
                var queryContext = new TestQueryContext(setting.Item1, 0, indexingInfo);
                var inputQueryText = queryText + setting.Item2;    
                var expectedResultText = queryText;

                var snQuery = parser.Parse(inputQueryText, queryContext);

                var visitor = new SnQueryToStringVisitor();
                visitor.Visit(snQuery.QueryTree);
                var actualResultText = visitor.Output;

                Assert.AreEqual(expectedResultText, actualResultText);
                Assert.AreEqual(setting.Item3, snQuery.Top);
                Assert.AreEqual(setting.Item4, snQuery.Skip);
                Assert.AreEqual(setting.Item5, snQuery.EnableAutofilters);
                Assert.AreEqual(setting.Item6, snQuery.EnableLifespanFilter);
                Assert.AreEqual(setting.Item7, snQuery.QueryExecutionMode);
                var sortIndex =  settings.IndexOf(setting);
                Assert.IsTrue((!snQuery.Sort.Any() && expectedSortInfo[sortIndex] == null) || expectedSortInfo[sortIndex].Count() == snQuery.Sort.Length);
            }
        }

        //UNDONE: Move this test to QueryClassifier tests
        //[TestMethod]
        //public void Search_Parser_UsedFieldNames()
        //{
        //    var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
        //    {
        //        {"Id", new TestPerfieldIndexingInfo_int() },
        //        {"Name", new TestPerfieldIndexingInfo_string() },
        //        {"Field1", new TestPerfieldIndexingInfo_string() },
        //        {"Field2", new TestPerfieldIndexingInfo_string() }
        //    };
        //    var queryContext = new TestQueryContext(QuerySettings.AdminSettings, 0, indexingInfo);
        //    var parser = new CqlParser();
        //    var queryText = "+Id:<1000 +Name:Admin* +(Field1:value1 Field2:value2) +(Field1:asdf)";
        //    var expected = "Field1, Field2, Id, Name";

        //    var snQuery = parser.Parse(queryText, queryContext);

        //    var actual = string.Join(", ", snQuery.QueryFieldNames.OrderBy(x => x).ToArray());
        //    Assert.AreEqual(expected, actual);
        //}

    }
}

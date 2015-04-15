using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using SiteSpider;

namespace TestSiteSpider
{
    public class DataParsingTests
    {
        [Fact]
        public void CleanData()
        {
            var spider = getSpider("http://some.com/");
            string source;
            string test;

            //page without script
            test = spider.cleanData("some <b>text</b>");
            Assert.Equal(test, "some <b>text</b>");

            //page with script src
            source = "some <script src='test.js'></script>";
            test = spider.cleanData(source);
            Assert.Equal(test, source);

            //page with script src
            source = "some <script src=\"test.js\"></script>";
            test = spider.cleanData(source);
            Assert.Equal(test, source);

            //page with script src
            source = "some <script src=\"test.js\" type=\"text\"></script>";
            test = spider.cleanData(source);
            Assert.Equal(test, source);

            //page with script tag
            source = "some <script type=\"text\"> test </script>";
            test = spider.cleanData(source);
            Assert.Equal(test, "some ");

            //page with script tag
            source = "some <script type='text'> test </script>";
            test = spider.cleanData(source);
            Assert.Equal(test, "some ");

            //page with script tag
            source = "some <script> test </script>";
            test = spider.cleanData(source);
            Assert.Equal(test, "some ");

            //page with script tag
            source = "some <script> <script>test<sc + ript> </script>";
            test = spider.cleanData(source);
            Assert.Equal(test, "some ");

            //page with script tag
            source = "some <script type=\"text/javascript\" charset=\"utf-8\"> test </script>";
            test = spider.cleanData(source);
            Assert.Equal(test, "some ");

            
        }

        private Spider getSpider(string url)
        {
            return new Spider(null, url);
        }
    }
}

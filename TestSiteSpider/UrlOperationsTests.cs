using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using SiteSpider;

namespace TestSiteSpider
{
    public class UrlOperationsTests
    {
        [Fact]
        public void StringToLink()
        {
            var spider = getSpider("http://some.com/");
            string test;

            //url from site top
            test = spider.StringToLink("/test1.html", "http://some.com/test.html").Url;
            Assert.Equal(test, "http://some.com/test1.html");

            //not specified protocol
            test = spider.StringToLink("//some.com/test2.html", "http://some.com/test.html").Url;
            Assert.Equal(test, "http://some.com/test2.html");

            //http:// url
            test = spider.StringToLink("http://some.com/test3.html", "http://some.com/test.html").Url;
            Assert.Equal(test, "http://some.com/test3.html");

            //https:// url
            test = spider.StringToLink("https://some.com/test4.html", "http://some.com/test.html").Url;
            Assert.Equal(test, "https://some.com/test4.html");
        }

        [Fact]
        public void StringToLinkRelative()
        {
            var spider = getSpider("http://some.com/");
            string test;

            //relative url
            test = spider.StringToLink("other.html", "http://some.com/test.html").Url;
            Assert.Equal(test, "http://some.com/other.html");
        }

        [Fact]
        public void StringToLinkRelativeFolder()
        {
            var spider = getSpider("http://some.com/");
            string test;

            //relative url
            test = spider.StringToLink("../other.html", "http://some.com/folder/test.html").Url;
            Assert.Equal(test, "http://some.com/other.html");
        }

        [Fact]
        public void StringToLinkJavascript()
        {
            var spider = getSpider("http://some.com/");
            string test;

            //relative url
            test = spider.StringToLink("javascript:do_some();", "http://some.com/folder/test.html").Url;
            Assert.Equal(test, null);
        }

        [Fact]
        public void StringToLinkInner()
        {
            var spider = getSpider("http://some.com/");
            string test;

            //starts with #
            test = spider.StringToLink("#some", "http://some.com/test.html").Url;
            Assert.Equal(test, null);

            //contains with #
            test = spider.StringToLink("https://some.com/test6.html#some", "http://some.com/test.html").Url;
            Assert.Equal(test, "https://some.com/test6.html");
        }

        private Spider getSpider(string url)
        {
            return new Spider(null, url);
        }

    }
}

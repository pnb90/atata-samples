﻿using System;
using Atata;
using NUnit.Framework;

namespace AtataSamples.MultipleBrowsersViaFixtureArguments
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            AtataContext.GlobalConfiguration.
                UseChrome().
                    WithArguments("start-maximized").
                UseInternetExplorer().
                    // TODO: Specify Internet Explorer settings, like:
                    // WithOptions(x => x.EnableNativeEvents = true).
                    WithDriverPath(Environment.GetEnvironmentVariable("IEWebDriver") ?? AppDomain.CurrentDomain.BaseDirectory).
                UseFirefox().
                    // TODO: You can also specify remote driver configuration(s):
                    WithDriverPath(Environment.GetEnvironmentVariable("GeckoWebDriver") ?? AppDomain.CurrentDomain.BaseDirectory).
                // UseRemoteDriver().
                // WithAlias("chrome_remote").
                // WithRemoteAddress("http://127.0.0.1:4444/wd/hub").
                // WithOptions(new ChromeOptions()).
                UseBaseUrl("https://atata-framework.github.io/atata-sample-app/#!/").
                AddNUnitTestContextLogging().
                LogNUnitError();
        }
    }
}

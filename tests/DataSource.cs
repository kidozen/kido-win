using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using KidoZen;

#if WINDOWS_PHONE
using Microsoft.Phone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#elif NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

namespace test
{
    [TestClass]
    [Tag("datasource")]
    public class DataSource : WorkItemTest
	{
		static KZApplication app;
		static KidoZen.DataSource queryDataSrc; 
        static KidoZen.DataSource invokeDataSrc;

        [ClassInitialize]
        [Asynchronous]
#if WINDOWS_PHONE
        public async Task ClassInit()
#elif NETFX_CORE
        static public async Task ClassInit(TestContext context)
#endif
        {
			if (app==null) {
				app = new KZApplication(Constants.MarketplaceUrl, Constants.AppName);
                await app.Initialize();
				var user = await app.Authenticate(Constants.User, Constants.Password, Constants.Provider);
			}
			if (queryDataSrc == null) {
				queryDataSrc = app.DataSource["test-query"];
			}
			if (invokeDataSrc == null) {
				invokeDataSrc = app.DataSource["test-operation"];
			}
            EnqueueTestComplete();
        }

        [TestMethod]
        public void CanGetAnInstance()
		{
			Assert.AreEqual(Constants.AppUrl + "/api/v2/datasources/test-query", queryDataSrc.Url.ToString());
		}

        [TestMethod]
        [Asynchronous]
        public async Task Get()
		{
			var getResult = await queryDataSrc.Query();
			Assert.AreEqual(HttpStatusCode.OK, getResult.StatusCode);
			Assert.IsNotNull(getResult.Data);
            EnqueueTestComplete();
		}

        [TestMethod]
        [Asynchronous]
        public async Task Invoke()
		{
			var invokeResult = await invokeDataSrc.Invoke();
			Assert.AreEqual(HttpStatusCode.OK, invokeResult.StatusCode);
			Assert.IsNotNull(invokeResult.Data);
            EnqueueTestComplete();
        }
	}
}

using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace SmartTools.CopyMove
{
    public class Utils
    {
        public static NameValueCollection Start(string pipelineName, ClientPipelineArgs args, Database database, Item[] items)
        {
            Assert.ArgumentNotNullOrEmpty(pipelineName, "pipelineName");
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(database, "database");
            Assert.ArgumentNotNull(items, "items");
            NameValueCollection values = new NameValueCollection();
            ListString str = new ListString('|');
            for (int i = 0; i < items.Length; i++)
            {
                str.Add(items[i].ID.ToString());
            }
            values.Add("database", database.Name);
            values.Add("items", str.ToString());
            args.Parameters = values;
            Context.ClientPage.Start(pipelineName, args);
            return values;
        }

        public static NameValueCollection Start(string pipelineName, Database database, Item[] items)
        {
            Assert.ArgumentNotNullOrEmpty(pipelineName, "pipelineName");
            Assert.ArgumentNotNull(database, "database");
            Assert.ArgumentNotNull(items, "items");
            ClientPipelineArgs args = new ClientPipelineArgs();
            Start(pipelineName, args, database, items);
            return args.Parameters;
        }
    }
}
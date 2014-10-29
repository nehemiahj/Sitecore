namespace SmartTools.CopyMove.Pipelines
{
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Jobs;
    using Sitecore.Links;
    using Sitecore.Shell.Framework.Pipelines;
    using Sitecore.Text;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.Collections.Generic;

    public class MoveSubItems : ItemOperation
    {
        public void CheckLinks(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            List<Item> items = GetSubItems(args);
            if (args.IsPostBack)
            {
                if (args.Result != "yes")
                {
                    args.AbortPipeline();
                }
            }
            else
            {
                int num = 0;
                foreach (Item item in items)
                {
                    num += ItemOperation.GetLinks(item);
                    if (num > 250)
                    {
                        break;
                    }
                }
                if (num > 250)
                {
                    SheerResponse.Confirm(Translate.Text("This operation may take a long time to complete.\n\nAre you sure you want to continue?"));
                    args.WaitForPostBack();
                }
            }
        }

        public void CheckPermissions(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (SheerResponse.CheckModified())
            {
                List<Item> items = GetSubItems(args);
                List<Item> list2 = new List<Item>();
                foreach (Item item in items)
                {
                    if (item.Appearance.ReadOnly)
                    {
                        list2.Add(item);
                    }
                }
                if (list2.Count > 0)
                {
                    if (list2.Count == 1)
                    {
                        SheerResponse.Alert(Translate.Text("You cannot move \"{0}\" because it is protected.", new object[] { list2[0].DisplayName }), new string[0]);
                    }
                    else
                    {
                        SheerResponse.Alert("You cannot move all of these items because one or more of them is protected.", new string[0]);
                    }
                    args.AbortPipeline();
                }
                List<Item> list3 = new List<Item>();
                foreach (Item item2 in items)
                {
                    if (!item2.Access.CanDelete())
                    {
                        list3.Add(item2);
                    }
                }
                if (list3.Count > 0)
                {
                    if (list3.Count == 1)
                    {
                        SheerResponse.Alert(Translate.Text("You do not have permission to move \"{0}\".", new object[] { list3[0].DisplayName }), new string[0]);
                    }
                    else
                    {
                        SheerResponse.Alert("You do not have permission to move all of these items.", new string[0]);
                    }
                    args.AbortPipeline();
                }
            }
        }

        public void CheckShadows(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (args.Result == "yes")
                {
                    args.Result = string.Empty;
                    args.IsPostBack = false;
                }
                else if (args.Result == "no")
                {
                    args.AbortPipeline();
                }
            }
            else
            {
                Database database = GetDatabase(args);
                Item target = GetTarget(args);
                ListString list = new ListString(args.Parameters["items"], '|');
                if (!IsSameDatabases(target, list, database))
                {
                    SheerResponse.Alert("One or more items are from another database, and you cannot move\nthese items outside their database.", new string[0]);
                    args.AbortPipeline();
                }
                else if (HasShadows(list, database))
                {
                    string str2;
                    if (list.Count == 1)
                    {
                        Item item = database.GetItem(list[0]);
                        if (item == null)
                        {
                            SheerResponse.Alert("Item not found.", new string[0]);
                            args.AbortPipeline();
                            return;
                        }
                        str2 = Translate.Text("This item also occurs in other locations. If you move it,\nit maybe deleted from the other locations.\n\nAre you sure you want to move '{0}'?", new object[] { item.Name });
                    }
                    else
                    {
                        str2 = Translate.Text("One or more items also occur in other locations.\nIf you move these items,\nthey maybe deleted from the other locations.\n\nAre you sure you want to move these items?");
                    }
                    SheerResponse.Confirm(str2);
                    args.WaitForPostBack();
                }
            }
        }

        public void Execute(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            //Move only subitems
            List<Item> items = GetSubItems(args);
            Assert.IsNotNull(items, typeof(List<Item>));
            Item target = GetTarget(args);
            Assert.IsNotNull(target, typeof(Item));
            string str = target.Uri.ToString();
            foreach (Item item2 in items)
            {
                Log.Audit(this, "Move sub item: {0} to {1}", new string[] { AuditFormatter.FormatItem(item2), str });
                item2.MoveTo(target);
            }
        }

        private static Database GetDatabase(ClientPipelineArgs args)
        {
            Database database = Factory.GetDatabase(args.Parameters["database"]);
            Assert.IsNotNull(database, typeof(Database), "Database: {0}", new object[] { args.Parameters["database"] });
            return database;
        }

        public void GetDestination(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Database database = GetDatabase(args);
            if (args.Result == "undefined")
            {
                args.AbortPipeline();
            }
            else if ((args.Result != null) && (args.Result.Length > 0))
            {
                args.Parameters["target"] = args.Result;
                Item item = database.GetItem(args.Result);
                Assert.IsNotNull(item, typeof(Item), "ID: {0}", new object[] { args.Result });
                if (!item.Access.CanCreate())
                {
                    Context.ClientPage.ClientResponse.Alert("You do not have permission to create items here.");
                    args.AbortPipeline();
                }
                args.IsPostBack = false;
            }
            else
            {
                ListString str = new ListString(args.Parameters["items"], '|');
                Item item2 = database.Items[str[0]];
                Assert.IsNotNull(item2, typeof(Item), "ID: {0}", new object[] { str[0] });
                UrlString str2 = new UrlString("/sitecore/shell/Applications/Dialogs/SmartMoveToSubItems.aspx");
                str2.Append("fo", item2.ID.ToString());
                str2.Append("sc_content", item2.Database.Name);
                Context.ClientPage.ClientResponse.ShowModalDialog(str2.ToString(), true);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Mostly not used. 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static List<Item> GetTargetItems(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            List<Item> list = new List<Item>();
            Database database = GetDatabase(args);
            ListString str = new ListString(args.Parameters["items"], '|');
            foreach (string str2 in str)
            {
                Item item = database.Items[str2];
                if (item != null)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        private static List<Item> GetSubItems(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            List<Item> list = new List<Item>();
            Database database = GetDatabase(args);
            ListString str = new ListString(args.Parameters["items"], '|');
            foreach (string str2 in str)
            {
                Item item = database.Items[str2];

                //Has children? Add it.
                if (item != null && item.HasChildren)
                {
                    foreach (Item child in item.Children)
                    {
                        list.Add(child);
                    }
                }
            }
            return list;
        }


        private static Item GetTarget(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Item item = GetDatabase(args).Items[args.Parameters["target"]];
            Assert.IsNotNull(item, typeof(Item), "ID: {0}", new object[] { args.Parameters["target"] });
            return item;
        }

        private static bool HasShadows(ListString list, Database database)
        {
            Assert.ArgumentNotNull(list, "list");
            Assert.ArgumentNotNull(database, "database");
            foreach (string str in list)
            {
                Item item = database.GetItem(str);
                if ((item != null) && (item.RuntimeSettings.IsVirtual || item.Database.DataManager.HasShadows(item)))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsSameDatabases(Item target, ListString list, Database database)
        {
            Assert.ArgumentNotNull(target, "target");
            Assert.ArgumentNotNull(list, "list");
            Assert.ArgumentNotNull(database, "database");
            foreach (string str in list)
            {
                Item item = database.GetItem(str);
                if ((item != null) && (target.RuntimeSettings.OwnerDatabase.Name != item.RuntimeSettings.OwnerDatabase.Name))
                {
                    return false;
                }
            }
            return true;
        }

        public void RepairLinks(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            List<Item> items = GetSubItems(args);
            Assert.IsNotNull(items, typeof(List<Item>));
            foreach (Item item in items)
            {
                JobOptions options = new JobOptions("LinkUpdater", "LinkUpdater", Context.Site.Name, new LinkUpdaterJob(item), "Update")
                {
                    ContextUser = Context.User
                };
                JobManager.Start(options);
            }
        }
    }
}


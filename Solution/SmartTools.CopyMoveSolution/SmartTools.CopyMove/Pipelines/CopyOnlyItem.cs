namespace SmartTools.CopyMove.Pipelines
{
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Shell.Framework.Pipelines;
    using Sitecore.Text;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class CopyOnlyItem
    {
        public void CheckDestination(CopyItemsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!args.HasResult)
            {
                args.AbortPipeline();
            }
            else
            {
                Database database = GetDatabase(args);
                if (((args.Result != null) && (args.Result.Length > 0)) && (args.Result != "undefined"))
                {
                    if (!database.GetItem(args.Result).Access.CanCreate())
                    {
                        Context.ClientPage.ClientResponse.Alert("You do not have permission to create items here.");
                        args.AbortPipeline();
                        return;
                    }
                    args.Parameters["destination"] = args.Result;
                }
                args.IsPostBack = false;
            }
        }

        public void CheckLanguage(CopyItemsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (args.Result != "yes")
                {
                    args.AbortPipeline();
                }
            }
            else
            {
                bool flag = false;
                foreach (Item item in GetItems(args))
                {
                    if (item.TemplateID == TemplateIDs.Language)
                    {
                        flag = true;
                        break;
                    }
                }
                if (flag)
                {
                    SheerResponse.Confirm("You are coping a language.\n\nA language item must have a name that is a valid ISO-code.\n\nPlease rename the copied item afterward.\n\nAre you sure you want to continue?");
                    args.WaitForPostBack();
                }
            }
        }

        public virtual void Execute(CopyItemsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Item item = GetDatabase(args).GetItem(args.Parameters["destination"]);
            Assert.IsNotNull(item, args.Parameters["destination"]);
            ArrayList list = new ArrayList();
            List<Item> items = GetItems(args);
            string str = item.Uri.ToString();
            foreach (Item item2 in items)
            {
                if (item2 != null)
                {
                    Log.Audit(this, "Copy item: {0}", new string[] { AuditFormatter.FormatItem(item2), str });
                    string copyOfName = ItemUtil.GetCopyOfName(item, item2.Name);
                    Item item3 = Context.Workflow.CopyItem(item2, item, copyOfName, Sitecore.Data.ID.NewID, false);
                    list.Add(item3);
                }
            }
            args.Copies = list.ToArray(typeof(Item)) as Item[];
        }

        protected static Database GetDatabase(CopyItemsArgs args)
        {
            string name = args.Parameters["database"];
            Database database = Factory.GetDatabase(name);
            Assert.IsNotNull(database, name);
            return database;
        }

        public void GetDestination(CopyItemsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (SheerResponse.CheckModified())
            {
                Database database = GetDatabase(args);
                ListString str = new ListString(args.Parameters["items"], '|');
                Item item = database.Items[str[0]];
                UrlString str2 = new UrlString(this.GetDialogUrl());
                if (item != null)
                {
                    str2.Append("fo", item.ID.ToString());
                    str2.Append("sc_content", item.Database.Name);
                }
                Context.ClientPage.ClientResponse.ShowModalDialog(str2.ToString(), true);
                args.WaitForPostBack(false);
            }
        }

        protected virtual string GetDialogUrl()
        {
            return "/sitecore/shell/Applications/Dialogs/SmartCopyToItem.aspx";
        }

        protected static List<Item> GetItems(CopyItemsArgs args)
        {
            List<Item> list = new List<Item>();
            Database database = GetDatabase(args);
            ListString str = new ListString(args.Parameters["items"], '|');
            foreach (string str2 in str)
            {
                Item item = database.GetItem(str2);
                if (item != null)
                {
                    list.Add(item);
                }
            }
            return list;
        }
    }
}


namespace SmartTools.CopyMove
{
    using Sitecore;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.IO;
    using Sitecore.Shell.Framework;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Pages;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Web.UI.WebControls;
    using System;

    public class SmartCopyToSubItemsForm : DialogForm
    {
        protected Sitecore.Web.UI.HtmlControls.DataContext DataContext;
        protected Edit Filename;
        protected TreeviewEx Treeview;

        private Item GetCurrentItem(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            string str = message["id"];
            Item folder = this.DataContext.GetFolder();
            Language language = Context.Language;
            if (folder != null)
            {
                language = folder.Language;
            }
            if (!string.IsNullOrEmpty(str))
            {
                return Context.ContentDatabase.Items[str, language];
            }
            return folder;
        }

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            Dispatcher.Dispatch(message, this.GetCurrentItem(message));
            base.HandleMessage(message);
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                this.DataContext.GetFromQueryString();
                Context.ClientPage.ServerProperties["id"] = WebUtil.GetQueryString("fo");
                Item folder = this.DataContext.GetFolder();
                Assert.IsNotNull(folder, "Item not found");
                this.Filename.Value = this.ShortenPath(folder.Paths.Path);
            }
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            string str = this.Filename.Value;
            if (str.Length == 0)
            {
                SheerResponse.Alert("Select an item.", new string[0]);
            }
            Item root = this.DataContext.GetRoot();
            if ((root != null) && (root.ID != root.Database.GetRootItem().ID))
            {
                str = FileUtil.MakePath(root.Paths.Path, str, '/');
            }
            Item item = this.DataContext.GetItem(str);
            if (item == null)
            {
                SheerResponse.Alert("The target item could not be found.", new string[0]);
            }
            else
            {
                string str3 = Context.ClientPage.ServerProperties["id"] as string;
                if ((str3 != null) && (str3.Length > 0))
                {
                    Item item3 = Context.ContentDatabase.Items[str3];
                    if (item3 == null)
                    {
                        SheerResponse.Alert("The source item could not be found.", new string[0]);
                        return;
                    }
                    if (item.ID.ToString() == item3.ID.ToString())
                    {
                        SheerResponse.Alert("Select a different item.", new string[0]);
                        return;
                    }
                    if (item.Paths.LongID.StartsWith(item3.Paths.LongID, StringComparison.InvariantCulture))
                    {
                        SheerResponse.Alert("An item cannot be copied below itself.", new string[0]);
                        return;
                    }
                }
                if (!item.Access.CanCreate())
                {
                    SheerResponse.Alert("The item cannot be copied to this location because\nyou do not have Create permission.", new string[0]);
                }
                else
                {
                    SheerResponse.SetDialogValue(item.ID.ToString());
                    base.OnOK(sender, args);
                }
            }
        }

        protected void SelectTreeNode()
        {
            Item selectionItem = this.Treeview.GetSelectionItem();
            if (selectionItem != null)
            {
                this.Filename.Value = this.ShortenPath(selectionItem.Paths.Path);
            }
        }

        private string ShortenPath(string path)
        {
            Assert.ArgumentNotNull(path, "path");
            Item root = this.DataContext.GetRoot();
            if ((root != null) && (root.ID != root.Database.GetRootItem().ID))
            {
                string str = root.Paths.Path;
                if (path.StartsWith(str, StringComparison.InvariantCulture))
                {
                    path = StringUtil.Mid(path, str.Length);
                }
            }
            return path;
        }
    }
}


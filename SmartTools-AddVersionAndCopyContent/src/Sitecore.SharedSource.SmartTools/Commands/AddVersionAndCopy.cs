namespace Sitecore.SharedSource.SmartTools.Commands
{
    using Sitecore;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Text;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Workflows;
    using System;
    using System.Collections.Specialized;
    using System.Globalization;

    [Serializable]
    public class AddVersionAndCopy : Command
    {
        public override void Execute(CommandContext context)
        {
            try
            {
                Error.AssertObject(context, "context");
                if (context.Items.Length == 1)
                {
                    Item item = Context.ContentDatabase.GetItem(context.Items[0].ID, context.Items[0].Language, context.Items[0].Version);
                    CultureInfo info = new CultureInfo(StringUtil.GetString(Sitecore.Context.ClientPage.ServerProperties["TranslatingLanguage"]));
                    Item item2 = context.Items[0];
                    NameValueCollection parameters = new NameValueCollection();
                    parameters["id"] = item2.ID.ToString();
                    parameters["language"] = item2.Language.ToString();
                    parameters["version"] = item2.Version.ToString();
                    IWorkflow workflow = item2.Database.WorkflowProvider.GetWorkflow(item2);
                    Context.ClientPage.Start(this, "Run", parameters);
                }
            }
            catch (Exception exception)
            {
                Sitecore.Diagnostics.Log.Error(exception.Message, this);
            }
        }

        public override CommandState QueryState(CommandContext commandContext)
        {
            Error.AssertObject(commandContext, "context");
            if (commandContext.Items.Length != 1)
            {
                return CommandState.Disabled;
            }
            Item item = commandContext.Items[0];
            if (item.Appearance.ReadOnly)
            {
                return CommandState.Disabled;
            }
            if (!item.Access.CanRead())
            {
                return CommandState.Disabled;
            }
            return base.QueryState(commandContext);
        }

        protected void Run(ClientPipelineArgs args)
        {            
            try
            {
                string str = args.Parameters["id"];
                string name = args.Parameters["language"];
                string str3 = args.Parameters["version"];
                Item item = Context.ContentDatabase.Items[str, Language.Parse(name), Sitecore.Data.Version.Parse(str3)];
                Error.AssertItemFound(item);
                if (SheerResponse.CheckModified())
                {
                    if (args.IsPostBack)
                    {
                        if (args.Result == "yes")
                        {
                            Context.ClientPage.SendMessage(this, "item:load(id=" + str + ",language=" + name + ",version=" + str3 + ")");
                        }
                    }
                    else
                    {
                        UrlString str4 = new UrlString(UIUtil.GetUri("control:AddVersionAndCopy"));
                        str4.Add("id", item.ID.ToString());
                        str4.Add("la", item.Language.ToString());
                        str4.Add("vs", item.Version.ToString());
                        str4.Add("ci", item.Language.ToString());
                        SheerResponse.ShowModalDialog(str4.ToString(), true);
                        args.WaitForPostBack();
                    }
                }
            }
            catch (Exception exception)
            {
                Sitecore.Diagnostics.Log.Error(exception.Message, this);
            }
        }
    }
}


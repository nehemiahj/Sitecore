namespace SmartTools.CopyMove.Command
{
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Shell.Framework;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Shell.Framework.Pipelines;
    using System;

    [Serializable]
    public class CopyOnlyItem : Command
    {
        public override void Execute(CommandContext context)
        {
            Item[] items = context.Items;
            Assert.ArgumentNotNull(items, "items");
            if (items.Length > 0)
            {
                CopyItemsArgs args = new CopyItemsArgs();
                Utils.Start("uiCopyOnlyItem", args, items[0].Database, items);
            }
        }

        public override CommandState QueryState(CommandContext context)
        {
            Error.AssertObject(context, "context");
            if (context.Items.Length != 1)
            {
                return CommandState.Disabled;
            }
            Item item = context.Items[0];
            if (item.Appearance.ReadOnly)
            {
                return CommandState.Disabled;
            }
            if (!item.Access.CanRead())
            {
                return CommandState.Disabled;
            }
            return base.QueryState(context);
        }
    }
}
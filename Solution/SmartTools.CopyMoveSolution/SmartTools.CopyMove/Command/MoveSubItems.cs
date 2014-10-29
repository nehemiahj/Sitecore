namespace SmartTools.CopyMove.Command
{
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Shell.Framework;
    using Sitecore.Shell.Framework.Commands;
    using System;

    [Serializable]
    public class MoveSubItems : Command
    {
        public override void Execute(CommandContext context)
        {
            using (new StatisticDisabler(StatisticDisablerState.ForItemsWithoutVersionOnly))
            {
                Item[] items = context.Items;
                Assert.ArgumentNotNull(items, "items");
                if (items.Length > 0)
                {
                    Utils.Start("uiMoveSubItems", items[0].Database, items);
                }
            }
        }

        public override CommandState QueryState(CommandContext context)
        {
            Error.AssertObject(context, "context");
            if (context.Items.Length == 0)
            {
                return CommandState.Disabled;
            }
            if (!context.Items[0].HasChildren)
            {
                return CommandState.Disabled;
            } 
            foreach (Item item in context.Items)
            {
                if (item.Appearance.ReadOnly)
                {
                    return CommandState.Disabled;
                }
                if (!item.Access.CanDelete())
                {
                    return CommandState.Disabled;
                }
                if (Command.IsLockedByOther(item))
                {
                    return CommandState.Disabled;
                }
            }
            return base.QueryState(context);
        }
    }
}


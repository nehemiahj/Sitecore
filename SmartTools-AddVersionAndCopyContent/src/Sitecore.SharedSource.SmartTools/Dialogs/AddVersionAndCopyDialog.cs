namespace Sitecore.SharedSource.SmartTools.Dialogs
{
    using Microsoft.ApplicationBlocks.Data;
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Data.Managers;
    using Sitecore.Diagnostics;
    using Sitecore.Shell.Applications.ContentEditor;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Pages;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using Sitecore.Collections;
    using Sitecore.Globalization;
    using Sitecore.SecurityModel;
    using Sitecore.Shell.Applications.Dialogs.ProgressBoxes;
    using Sitecore.Layouts;
    using System.Text.RegularExpressions;

    public class AddVersionAndCopyDialog : DialogForm
    {
        protected Language sourceLanguage;
        protected bool CopySubItems;

        protected Dictionary<string, string> langNames;
        protected Combobox Source;
        protected Literal TargetLanguages;
        protected Literal Options;
        protected TreeList TreeListOfItems;
        protected List<Language> targetLanguagesList;

        private void fillLanguageDictionary()
        {
            this.langNames = new Dictionary<string, string>();

            LanguageCollection languages;
            Database database = Context.ContentDatabase;
            languages = LanguageManager.GetLanguages(database);

            foreach (Language language in languages)
            {
                this.langNames.Add(language.CultureInfo.EnglishName, language.Name);//Danish - Key, da - Value
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                Assert.ArgumentNotNull(e, "e");
                base.OnLoad(e);
                if (!Context.ClientPage.IsEvent)
                {
                    Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
                    ListItem child = new ListItem();
                    this.Source.Controls.Add(child);
                    CultureInfo info = new CultureInfo(Context.Request.QueryString["ci"]);
                    child.Header = info.EnglishName;
                    child.Value = info.EnglishName;
                    child.ID = Control.GetUniqueID("I");

                    if (itemFromQueryString == null)
                        throw new Exception();

                    string str = "<script type='text/javascript'>function toggleChkBoxMethod2(formName){var form=$(formName);var i=form.getElements('checkbox'); i.each(function(item){item.checked = !item.checked;});$('togglechkbox').checked = !$('togglechkbox').checked;}</script>";
                    str = str + "<table class='scFormTable'><tr></tr><tr></tr><tr></tr>";
                    this.fillLanguageDictionary();
                    foreach (KeyValuePair<string, string> pair in this.langNames)
                    {
                        if (itemFromQueryString.Language.Name != pair.Value)
                        {
                            string str2 = "chk_" + pair.Value;
                            string str4 = str;
                            str = str4 + "<tr><td>" + pair.Key + "</td><td>" + pair.Value + "</td><td><input class='reviewerCheckbox' style='padding: 5px;' type='checkbox' value='1' name='" + str2 + "'/></td></tr>";
                        }
                    }
                    str = str + "</table>";
                    this.TargetLanguages.Text = str;

                    //Options
                    str = "";
                    str += "<table  class='scFormTable'>";
                    str += "<tr><td>Include SubItems:</td><td><input class='optionsCheckbox' style='padding: 5px;' type='checkbox' value='1' name='chk_IncludeSubItems'/></td></tr>";
                    str += "</table>";
                    this.Options.Text = str;
                }
            }
            catch (Exception exception)
            {
                Sitecore.Diagnostics.Log.Error(exception.Message, this);
            }
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Exception exception;
            Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
            this.fillLanguageDictionary();
            targetLanguagesList = new List<Language>();
            try
            {
                //Get the source language
                if (itemFromQueryString == null)
                    throw new Exception();
                sourceLanguage = itemFromQueryString.Language;
                Sitecore.Diagnostics.Log.Debug("Smart Tools: OnOK-sourceLanguage-" + sourceLanguage.Name, this);

                //Get the target languages
                foreach (KeyValuePair<string, string> pair in this.langNames)
                {
                    if (!string.IsNullOrEmpty(Context.ClientPage.Request.Params.Get("chk_" + pair.Value)))
                    {
                        targetLanguagesList.Add(Sitecore.Data.Managers.LanguageManager.GetLanguage(pair.Value));
                    }
                }

                //Include SubItems?
                if (!string.IsNullOrEmpty(Context.ClientPage.Request.Params.Get("chk_IncludeSubItems")))
                {
                    CopySubItems = true;
                }
                Sitecore.Diagnostics.Log.Debug("Smart Tools: OnOK-CopySubItems-" + CopySubItems.ToString(), this);

                //Execute the process
                if (itemFromQueryString != null && targetLanguagesList.Count > 0 && sourceLanguage != null)
                {
                    //Execute the Job
                    Sitecore.Shell.Applications.Dialogs.ProgressBoxes.ProgressBox.Execute("Add Version and Copy", "Smart Tools", new ProgressBoxMethod(ExecuteOperation), itemFromQueryString);
                }
                else
                {
                    //Show the alert
                    Context.ClientPage.ClientResponse.Alert("Context Item and Target Languages are empty.");
                    Context.ClientPage.ClientResponse.CloseWindow();
                }

                Context.ClientPage.ClientResponse.Alert("Process has been completed.");
                Context.ClientPage.ClientResponse.CloseWindow();
            }
            catch (Exception exception8)
            {
                exception = exception8;
                Sitecore.Diagnostics.Log.Error(exception.Message, this);
                Context.ClientPage.ClientResponse.Alert("Exception Occured. Please check the logs.");
                Context.ClientPage.ClientResponse.CloseWindow();
            }
        }

        protected void ExecuteOperation(params object[] parameters)
        {
            Sitecore.Diagnostics.Log.Debug("Smart Tools: Job Executed.", this);

            if (parameters == null || parameters.Length == 0)
                return;

            Item item = (Item)parameters[0];
            IterateItems(item, targetLanguagesList, sourceLanguage);
        }

        private void IterateItems(Item item, List<Language> targetLanguages, Language sourceLang)
        {
            AddVersionAndCopyItems(item, targetLanguages, sourceLang);

            if (CopySubItems && item.HasChildren)
            {
                foreach (Item childItem in item.Children)
                {
                    IterateItems(childItem, targetLanguages, sourceLang);
                }
            }
        }

        private void AddVersionAndCopyItems(Item item, List<Language> targetLanguages, Language sourceLang)
        {
            foreach (Language language in targetLanguages)
            {
                Item source = Context.ContentDatabase.GetItem(item.ID, sourceLang);
                Item target = Context.ContentDatabase.GetItem(item.ID, language);

                if (source == null || target == null) return;

                Sitecore.Diagnostics.Log.Debug("Smart Tools: AddVersionAndCopyItems-SourcePath-" + source.Paths.Path, this);
                Sitecore.Diagnostics.Log.Debug("Smart Tools: AddVersionAndCopyItemsSourceLanguage-" + sourceLang.Name, this);
                Sitecore.Diagnostics.Log.Debug("Smart Tools: AddVersionAndCopyItems-TargetLanguage-" + language.Name, this);

                source = source.Versions.GetLatestVersion();
                target = target.Versions.AddVersion();

                target.Editing.BeginEdit();

                source.Fields.ReadAll();

                foreach (Field field in source.Fields)
                {
                    if (!field.Shared)
                    {
                        target[field.Name] = source[field.Name];
                    }
                }

                target[Sitecore.FieldIDs.FinalLayoutField] = source[Sitecore.FieldIDs.FinalLayoutField];
                target[Sitecore.FieldIDs.Workflow] = source[Sitecore.FieldIDs.Workflow];
                target[Sitecore.FieldIDs.WorkflowState] = source[Sitecore.FieldIDs.WorkflowState];

                target.Editing.EndEdit();

                // do all or only the ones through layouts?
                // all is easier.
                var contentItem = Context.ContentDatabase.GetItem(target.Paths.FullPath + "/Content");

                if (contentItem != null)
                {
                    var childItems = contentItem.Axes.GetDescendants();

                    bool oldFilteringValue = Sitecore.Context.Site.DisableFiltering; // this is needed because AddVersion will return NULL if this isn't true.
                    try
                    {
                        Sitecore.Context.Site.DisableFiltering = true;

                        foreach (var childItem in childItems)
                        {
                            var childItemWithLanguage = Context.ContentDatabase.GetItem(childItem.ID, language);
                            var newItemVersion = childItemWithLanguage.Versions.AddVersion();

                            // Copy workflow from Page item.
                            newItemVersion.Editing.BeginEdit();
                            newItemVersion[Sitecore.FieldIDs.Workflow] = target[Sitecore.FieldIDs.Workflow];
                            newItemVersion[Sitecore.FieldIDs.WorkflowState] = target[Sitecore.FieldIDs.WorkflowState];
                            newItemVersion.Editing.EndEdit();
                        }
                    }
                    finally
                    {
                        Sitecore.Context.Site.DisableFiltering = oldFilteringValue;
                    }
                }

                

                Sitecore.Diagnostics.Log.Debug("Smart Tools: AddVersionAndCopyItems-Completed.", this);
            }
        }
    }
}


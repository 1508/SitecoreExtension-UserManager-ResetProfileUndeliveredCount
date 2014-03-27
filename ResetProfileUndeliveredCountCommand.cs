using System.Collections.Specialized;
using Sitecore;
using Sitecore.Caching;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Modules.EmailCampaign;
using Sitecore.Security.Accounts;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Framework.Commands.UserManager;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XamlSharp.Continuations;

namespace SitecoreExtension.UserManager.ResetProfileUndeliveredCount
{
    public class ResetProfileUndeliveredCountCommand : Command, ISupportsContinuation
    {
        /// <summary>
        /// Executes the command in the specified context.
        /// 
        /// </summary>
        /// <param name="context">The context.</param><contract><requires name="context" condition="not null"/></contract>
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            string userName = context.Parameters["username"];
            if (ValidationHelper.ValidateUserWithMessage(userName))
            {
                NameValueCollection parameters = new NameValueCollection();
                parameters["username"] = userName;
                ClientPipelineArgs args = new ClientPipelineArgs(parameters);
                ContinuationManager.Current.Start(this, "Run", args);
            }
        }

        protected void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            ListString str = new ListString(args.Parameters["username"]);
            if (args.IsPostBack)
            {
                if (args.Result == "yes")
                {
                    foreach (string str2 in str)
                    {
                        Contact contactFromName = Util.GetContactFromName(str2);
                        if (contactFromName.Profile.UndeliveredCount != 0)
                        {
                            contactFromName.Profile.UndeliveredCount = 0;
                            contactFromName.Profile.Save();
                        }
                        RegistryCache registryCache = CacheManager.GetRegistryCache(Context.Site);
                        if (registryCache != null)
                        {
                            registryCache.Clear();
                        }
                        Log.Audit(this, "Reset Profile Undelivered Count: {0}", new string[] { str2 });
                    }
                    AjaxScriptManager.Current.Dispatch("usermanager:refresh");
                }
            }
            else
            {
                if (str.Count == 1)
                {
                    User user2 = User.FromName(str[0], true);
                    Assert.IsNotNull(user2, typeof(User));
                    SheerResponse.Confirm(Translate.Text("Are you sure you want to reset the Undelivered Count of {0} from {1} to 0 ?", new object[] { user2.GetLocalName(), GetUndeliveredCount(str[0]) }));
                }
                else
                {
                    SheerResponse.Confirm(Translate.Text("Are you sure you want to reset the Undelivered Count of these {0} users to 0 ?", new object[] { str.Count }));
                }
                args.WaitForPostBack();
            }
        }

        private int GetUndeliveredCount(string userName)
        {
            Contact contactFromName = Util.GetContactFromName(userName);
            return contactFromName.Profile.UndeliveredCount;
        }
    }
}
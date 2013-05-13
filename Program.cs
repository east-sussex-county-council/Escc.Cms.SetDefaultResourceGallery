using System;
using System.Collections.Generic;
using EsccWebTeam.Cms;
using EsccWebTeam.Cms.Permissions;
using Microsoft.ApplicationBlocks.ExceptionManagement;
using Microsoft.ContentManagement.Publishing;

namespace Escc.Cms.SetDefaultResourceGallery
{
    /// <summary>
    /// For the whole CMS site, sets the default resource gallery for each channel to one matching the name of a group with editor permissions for the channel, if it exists.
    /// </summary>
    /// <remarks>
    /// <para>The aim of this tool is to give web authors a hint as to which resource gallery to save resources in. It should be scheduled to run regularly as web author
    /// permissions, and therefore the appropriate resource gallery, can change. It is important that resources are uploaded to the correct gallery for the following scenario:</para>
    /// 
    /// <list type="bullet">
    /// <item>User A is in Groups 1 and 2, for two sets of pages</item>
    /// <item>User B is only in Group 2</item>
    /// <item>If User A saves a resource in the Group 1 gallery which should be in the Group 2 gallery, User B cannot save the page using that resource</item>
    /// </list>
    /// </remarks>
    class Program
    {
        private static readonly IList<string> GroupsWithoutAResourceGallery = new List<string>();

        static void Main(string[] args)
        {
            try
            {
                var traverser = new CmsTraverser();
                traverser.TraversingChannel += new CmsEventHandler(traverser_TraversingChannel);
                traverser.TraverseSite(PublishingMode.Update, true);
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
                throw;
            }
        }

        static void traverser_TraversingChannel(object sender, CmsEventArgs e)
        {
            Console.WriteLine("Channel: " + e.Channel.UrlModePublished);
            var groups = CmsPermissions.ReadCmsGroupsForChannel(e.Channel);

            foreach (var groupName in groups[CmsRole.Editor])
            {
                // Check for previous failed query, to avoid hundreds of checks on sitewide groups used by Digital Services
                if (GroupsWithoutAResourceGallery.Contains(groupName))
                {
                    continue;
                }

                var resourceGallery = e.Context.Searches.GetByPath("/Resources/Web authors/" + groupName) as ResourceGallery;
                if (resourceGallery != null)
                {
                    if (e.Channel.DefaultResourceGallery == null ||
                        e.Channel.DefaultResourceGallery.Guid != resourceGallery.Guid)
                    {
                        Console.WriteLine("Setting default resource gallery to " + resourceGallery.Name);
                        e.Channel.DefaultResourceGallery = resourceGallery;
                        e.Context.CommitAll();
                    }

                    return;
                }
                else
                {
                    GroupsWithoutAResourceGallery.Add(groupName);
                }
            }
        }
    }
}

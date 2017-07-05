namespace Sitecore.Support.Pipelines.HttpRequest
{
  using Data.Fields;
  using Sitecore;
  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Pipelines.HttpRequest;
  using System;

  public class AliasResolver : HttpRequestProcessor
  {
    public override void Process(HttpRequestArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (!Settings.AliasesActive)
      {
        Tracer.Warning("Aliases are not active.");
      }
      else
      {
        Database database = Context.Database;
        if (database == null)
        {
          Tracer.Warning("There is no context database in AliasResover.");
        }
        else
        {
          Profiler.StartOperation("Resolve alias.");
          if (database.Aliases.Exists(args.LocalPath) && !this.ProcessItem(args))
          {
            this.ProcessExternalUrl(args);
          }
          Profiler.EndOperation();
        }
      }
    }
    private void ProcessAliasQueryString(HttpRequestArgs args)
    {
      var aliasLink = Context.Database.Aliases[args.LocalPath] ?? null;
      if (aliasLink != null)
      {
        args.Url.QueryString = aliasLink.QueryString;
      }
    }

    private void ProcessExternalUrl(HttpRequestArgs args)
    {
      string targetUrl = Context.Database.Aliases.GetTargetUrl(args.LocalPath);
      if (targetUrl.Length > 0)
      {
        this.ProcessExternalUrl(targetUrl);
      }
    }

    private void ProcessExternalUrl(string path)
    {
      if (Context.Page.FilePath.Length <= 0)
      {
        Context.Page.FilePath = path;
      }
    }

    private bool ProcessItem(HttpRequestArgs args)
    {
      ID targetID = Context.Database.Aliases.GetTargetID(args.LocalPath);
      if (!targetID.IsNull)
      {
        Item target = args.GetItem(targetID);
        if (target != null)
        {
          this.ProcessItem(args, target);
          this.ProcessAliasQueryString(args);
        }
        return true;
      }
      Tracer.Error("An alias for \"" + args.LocalPath + "\" exists, but points to a non-existing item.");
      return false;
    }



    private void ProcessItem(HttpRequestArgs args, Item target)
    {
      if (Context.Item == null)
      {
        Context.Item = target;
        Tracer.Info(string.Concat(new object[] { "Using alias for \"", args.LocalPath, "\" which points to \"", target.ID, "\"" }));
      }
    }
  }
}

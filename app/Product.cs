using System;
using System.IO;
using PetFramework;

namespace Bruno
{
    internal static class Product
    {
        internal const string Repository="";
        internal const string AssetName="Bruno.exe";
        internal const string StartupName="Bruno";
        internal static ReleaseUpdater CreateUpdater()
        {
            if(String.IsNullOrEmpty(Repository))return null;
            return new ReleaseUpdater(new UpdateSettings{Repository=Repository,AssetName=AssetName,ProductName="Bruno Companion",DisplayName="Bruno",AppId="Bruno",Cache=Path.Combine(Preferences.Folder,"Updates"),WindowTitle="Bruno"});
        }
    }
}

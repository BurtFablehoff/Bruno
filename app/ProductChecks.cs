using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using PetFramework;
namespace Bruno
{
    internal static class ProductChecks
    {
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run(Assets art,string path) {
            Require(Product.Repository=="BurtFablehoff/Bruno"&&(Program.Updater!=null)==!Program.Development,"Updater identity/development isolation differs");
            object updaterChecks=Program.Development?null:Program.Updater.Test(Path.Combine(Path.GetDirectoryName(path),"updater-fixtures"));
            Require(!new Preferences().AutoUpdates&&!new Preferences().StartWithWindows,"Unexpected automatic update/startup defaults");
            var run=new Dictionary<string,string>();run["OtherPet"]="preserve";string exe=Assembly.GetExecutingAssembly().Location;
            Action<string,string> set=delegate(string key,string value){run[key]=value;};Action<string> remove=delegate(string key){run.Remove(key);};
            WindowsStartup.Change(Product.StartupName,true,exe,set,remove);Require(run["Bruno"]=="\""+exe+"\"","Startup quoting/identity differs");
            WindowsStartup.Change(Product.StartupName,false,exe,set,remove);Require(!run.ContainsKey("Bruno")&&run["OtherPet"]=="preserve","Startup touched another product");
            Require(FileVersionInfo.GetVersionInfo(exe).ProductName==(Program.Development?"Bruno Companion Development":"Bruno Companion"),"Product identity differs");
            Require(Preferences.Folder.EndsWith(Program.Development?"BrunoCompanionDevV1":"BrunoCompanionV1"),"Settings identity differs");
            for(int seed=0;seed<8;seed++) {
                var r=new Routine(art,seed);r.SelectAwake(seed%2==0?"sit":"lay");Require(r.Automatic,"Production pose choice disabled automatic sleep");
                for(int tick=0;tick<144000&&!r.Counts.ContainsKey("lay-to-sleep");tick++)r.Step(.1,false);
                Require(r.Counts.ContainsKey("lay-to-sleep"),"No autonomous sleep after manual awake choice");
                r.SelectAwake("sit");for(int i=0;i<40&&r.InTransition;i++)r.Step(.1,false);Require(r.Pose=="sit"&&r.Automatic,"Manual wake failed");
                bool rejected=false;try{r.SelectAwake("sleep");}catch(ArgumentException){rejected=true;}Require(rejected,"Production exposes manual sleep");
            }
            File.WriteAllText(path,new JavaScriptSerializer().Serialize(new{passed=true,development=Program.Development,product=FileVersionInfo.GetVersionInfo(exe).ProductName,updatesConfigured=!Program.Development,updaterCreated=Program.Updater!=null,updaterChecks=updaterChecks,startupQuoted=true,startupOptIn=true,startupRegistryName=Product.StartupName,settingsIsolated=true,autonomousSleepCases=8,manualSleepRejected=true,networkUsed=false,registryWritten=false,userSettingsWritten=false,uiOpened=false}));
        }
    }
}

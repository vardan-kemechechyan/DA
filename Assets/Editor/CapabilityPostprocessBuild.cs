using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
//using UnityEditor.iOS.Xcode;
using UnityEngine;

public class CapabilityPostprocessBuild
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS) return;

       /* // Read plist
        var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        // Update value
        PlistElementDict rootDict = plist.root;

        rootDict.SetString("NSUserTrackingUsageDescription", "This data will be used to improve the application analytics and to deliver more relevant ads.");
        //rootDict.SetString("NSContactUsageDescription", $"{Application.productName} requires access to your contacts.");
        //rootDict.SetString("NSCameraUsageDescription", "Uses the camera for Augmented Reality");
        //rootDict.SetString("NSPhotoLibraryUsageDescription", "${PRODUCT_NAME} photo use");
        //rootDict.SetString("NSPhotoLibraryAddUsageDescription", "${PRODUCT_NAME} photo use");
        rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        //rootDict.SetBoolean("NSAllowArbitraryLoads", true);

        // Write plist
        File.WriteAllText(plistPath, plist.WriteToString());

        string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
        ProjectCapabilityManager projCapability = new ProjectCapabilityManager(projPath, "Unity-iPhone/(entitlement file)", "Unity-iPhone");

        projCapability.AddPushNotifications(false);
        projCapability.AddBackgroundModes(BackgroundModesOptions.BackgroundFetch);

        projCapability.WriteToFile();*/
    }
}
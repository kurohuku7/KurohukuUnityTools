using System;
using Sirenix.OdinInspector;
using UnityEditor;
using System.IO;
using UnityEngine;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector.Editor;
using Debug = UnityEngine.Debug;

public class KurohukuTool : OdinEditorWindow
{
    private readonly string steamworksBuildUrl = "https://partner.steamgames.com/apps/builds/";
    private readonly string steamProductId = "1393780";
    private readonly string steamDemoId = "1429050";

    [MenuItem("Tools/kurohuku")]
    private static void OpenWindow()
    {
        GetWindow<KurohukuTool>().Show();
    }

    [Serializable]
    private struct VRDriverSetting
    {
        public bool enable;
        public int loadPriority;
        public string serialNumber;
        public string modelNumber;
        public int windowX;
        public int windowY;
        public int windowWidth;
        public int windowHeight;
        public int renderWidth;
        public int renderHeight;
        public float secondsFromVsyncToPhotons;
        public float displayFrequency;
    };

    [Serializable]
    private struct NullDriverSetting
    {
        public VRDriverSetting driver_null;
    }

    [Title("SteamVR")]

    [PropertyOrder(1)]
    public string steamVRPath = @"C:\Program Files (x86)\Steam\steamapps\common\SteamVR";

    [PropertyOrder(2)]
    [ShowInInspector]
    public bool nullDriver
    {
        get => EditorPrefs.GetBool("nullDriver");
        set
        {
            var settingPath = steamVRPath + @"\drivers\null\resources\settings\default.vrsettings";
            
            EditorPrefs.SetBool("nullDriver", value);
            
            // ファイルを書き換える
            if (File.Exists(settingPath))
            {
                var settingText = File.ReadAllText(settingPath);
                var settingObject = JsonUtility.FromJson<NullDriverSetting>(settingText);
                settingObject.driver_null.enable = value;
                File.WriteAllText(settingPath, JsonUtility.ToJson(settingObject));
            }
        }
    }

    [PropertyOrder(2)]
    [ShowInInspector]
    public bool launchOverlayViewer
    {
        get => EditorPrefs.GetBool("launchOverlayViewer");
        set => EditorPrefs.SetBool("launchOverlayViewer", value);
    }

    [PropertyOrder(3)]
    [Button]
    public async void RestartSteamVR()
    {
        var processes = Process.GetProcessesByName("vrmonitor");
        foreach (var process in processes)
        {
            process.CloseMainWindow();
            process.Close();
        }

        await UniTask.Delay(1000);

        Process.Start(steamVRPath + @"\bin\win64\vrmonitor.exe");
    }


    [PropertyOrder(4)]
    [Title("Deploy to Steam")]
    public string steamworksSdkPath = @"";

    [PropertyOrder(5)]
    public string buildPath = @"J:\UnityProjects\OVRLocomotionEffect\build";
    
    [PropertyOrder(5)]
    [Button]
    public void DeployDemo()
    {
        DeployToSteam(true);
    }

    [PropertyOrder(6)]
    [Button]
    public void DeployProduct()
    {
        DeployToSteam(false);
    }

    private void DeployToSteam(bool isDemo)
    {
        // content の中身を削除する
        Debug.Log(steamworksSdkPath);
        string contentPath = steamworksSdkPath + @"\tools\ContentBuilder\content";
        var contentDirectory = new DirectoryInfo(contentPath);
        foreach (var file in contentDirectory.GetFiles())
        {
            file.Delete();
        }
        foreach (var dir in contentDirectory.GetDirectories())
        {
            dir.Delete(true);
        }

        // バックアップファイルはデプロイする必要がないので削除
        var backupDirectoryPath = buildPath + @"\OVRLocomotionEffect_BackUpThisFolder_ButDontShipItWithYourGame";
        if (Directory.Exists(backupDirectoryPath))
        {
            Directory.Delete(backupDirectoryPath, true);
        }
        
        // アップロード用のフォルダにビルドしたファイルをコピー
        CopyDirectory(buildPath, contentPath);

        // script のバージョン番号を変更
        var scriptTemplatePath = steamworksSdkPath + @"\tools\ContentBuilder\scripts" + (isDemo ? @"\demo\app_build_1429050_template.vdf" : @"\main\app_build_1393780_template.vdf");
        var scriptTemplate = File.ReadAllText(scriptTemplatePath);
        var script = scriptTemplate.Replace("{{VERSION}}", $"v{Application.version}");
        var scriptPath = steamworksSdkPath + @"\tools\ContentBuilder\scripts" + (isDemo ? @"\demo\app_build_1429050.vdf" : @"\main\app_build_1393780.vdf");
        File.WriteAllText(scriptPath, script);

        // バッチファイルを実行
        var processInfo = new ProcessStartInfo();
        processInfo.FileName = steamworksSdkPath + @"\tools\ContentBuilder\builder\steamcmd.exe";
        processInfo.Arguments = @"+login kurohuku_build +run_app_build " + steamworksSdkPath + @"\tools\ContentBuilder\scripts" + (isDemo ? @"\demo\app_build_1429050.vdf" : @"\main\app_build_1393780.vdf") + " +quit";
        processInfo.UseShellExecute = true;
        processInfo.CreateNoWindow = false;

        var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new Exception($"Failed to run streamworks sdk deploy bat file. (isDemo={isDemo})");
        }
        process.WaitForExit();
        process.Close();
        
        Process.Start(new ProcessStartInfo
        {
            FileName = steamworksBuildUrl + (isDemo ? steamDemoId : steamProductId),
            UseShellExecute = true
        });
    }

    private void CopyDirectory(string src, string dst)
    {
        var dir = new DirectoryInfo(src);
        if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(dst);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(dst, file.Name);
            file.CopyTo(targetFilePath);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(dst, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }
}

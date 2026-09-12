// Exercise the production MediaLibrary with deterministic engine and transport doubles.
// This does not certify Unity rendering or a real network connection.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Wisp.Core;
using Wisp.UI;

namespace Newtonsoft.Json { public static class JsonConvert { public static T DeserializeObject<T>(string value) { return default(T); } } }
namespace UnityEngine
{
    public class Object { public bool Destroyed; public static void Destroy(Object o) { o.Destroyed=true; } }
    public class MonoBehaviour
    {
        public readonly List<IEnumerator> Jobs=new List<IEnumerator>();
        public object StartCoroutine(IEnumerator routine) { Jobs.Add(routine);routine.MoveNext();return routine; }
        public void Complete(int index,bool success,byte[] data)
        { var job=Jobs[index]; var request=(Networking.UnityWebRequest)job.Current; request.result=success?Networking.UnityWebRequest.Result.Success:Networking.UnityWebRequest.Result.Failure;request.downloadHandler.data=data;job.MoveNext(); }
    }
    public class Texture2D : Object
    {
        public int width,height; public FilterMode filterMode;
        public Texture2D(int w,int h,TextureFormat format,bool mipmaps) { width=w;height=h; }
    }
    public enum TextureFormat { RGBA32 } public enum FilterMode { Bilinear }
    public static class ImageConversion
    { public static bool LoadImage(Texture2D image,byte[] data,bool noRead) { if(data.Length!=8)return false;image.width=BitConverter.ToInt32(data,0);image.height=BitConverter.ToInt32(data,4);return image.width>0&&image.height>0; } }
    public static class Application { public static string persistentDataPath="."; }
    public static class Debug { public static void LogWarning(object message) {} }
}
namespace UnityEngine.Networking
{
    public class DownloadHandlerBuffer { public byte[] data; }
    public class UnityWebRequest : IDisposable
    {
        public enum Result { Success,Failure }
        public Result result; public int timeout;
        public bool Aborted,Disposed;
        public DownloadHandlerBuffer downloadHandler=new DownloadHandlerBuffer();
        public static UnityWebRequest Get(string url) { return new UnityWebRequest(); }
        public object SendWebRequest() { return this; }
        public void Abort() { Aborted=true; }
        public void Dispose() { Disposed=true; }
    }
}
public static class MediaHarness
{
    private static int count;
    private static void Check(bool condition,string message) { if(!condition)throw new Exception(message);count++; }
    private static byte[] Pixels(int width,int height) { var b=new byte[8];Buffer.BlockCopy(BitConverter.GetBytes(width),0,b,0,4);Buffer.BlockCopy(BitConverter.GetBytes(height),0,b,4,4);return b; }
    private static MediaLibrary Make(UnityEngine.MonoBehaviour host,string cache)
    {
        var type=typeof(MediaLibrary);
        var instance=(MediaLibrary)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        foreach(var field in type.GetFields(flags))
        {
            if(field.Name=="owner")field.SetValue(instance,host);
            else if(field.Name=="<Cache>k__BackingField")field.SetValue(instance,cache);
            else if(field.FieldType==typeof(ImageBudget))field.SetValue(instance,new ImageBudget());
            else if(field.FieldType.IsClass && field.FieldType.GetConstructor(Type.EmptyTypes)!=null)field.SetValue(instance,Activator.CreateInstance(field.FieldType));
        }
        return instance;
    }
    public static string Run()
    {
        count=0;
        string cache=Path.Combine(Path.GetTempPath(),"WispMedia-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(cache);
        string[] urls={"https://cdn.wikimg.net/a.png","https://cdn.wikimg.net/b.png","https://cdn.wikimg.net/c.png","https://cdn.wikimg.net/d.png"};
        try
        {
            var host=new UnityEngine.MonoBehaviour();var media=Make(host,cache);
            typeof(MediaLibrary).GetField("Regions").SetValue(media,new Dictionary<string,string>{{"city","embedded:locations/atlas-city.png"}});
            typeof(MediaLibrary).GetField("EnglishRegions").SetValue(media,new Dictionary<string,string>{{"city","embedded:locations/atlas-city.png"}});
            I18n.English=false;
            Check(media.RegionSource("city")=="embedded:locations/atlas-city.png","RU atlas selects packaged map without cache");
            I18n.English=true;
            Check(media.RegionSource("city")=="embedded:locations/atlas-city.png","English atlas does not bypass packaged maps");
            I18n.English=false;
            Check(media.RegionSource("city")=="embedded:locations/atlas-city.png" && host.Jobs.Count==0,"Language round trip preserves atlas source without network");
            var galleries=new Dictionary<string,StepImage[]> {
                { "well",new[]{new StepImage {Url="a"}} },
                { "speed/pdf-b1/well",new StepImage[0] },
                { "112/pdf-a4/nail-upgrade",new[]{new StepImage {Url="edited"}} }
            };
            typeof(MediaLibrary).GetField("Steps").SetValue(media,galleries);
            StepImage[] found;
            Check(media.TryStepImages("speed","pdf-b1","well",out found) && found.Length==0,"Empty occurrence hides inherited gallery");
            Check(media.TryStepImages("112","pdf-a1","well",out found) && found.Length==1 && found[0].Url=="a","A keeps shared gallery after B deletion");
            Check(media.TryStepImages("steel","pdf-c1","well",out found) && found.Length==1,"C keeps shared gallery after B deletion");
            Check(media.TryStepImages("112","pdf-a4","nail-upgrade",out found) && found[0].Url=="edited","Occurrence selects edited image and status URL together");

            foreach(var url in urls)media.Get(url);
            Check(host.Jobs.Count==3,"Actual loader limits active requests");
            I18n.English=true;host.Complete(0,true,Pixels(64,64));media.Get(urls[3]);
            Check(host.Jobs.Count==4,"Actual queued URL loads after language switch");
            host.Complete(1,false,new byte[0]);
            Check(media.CanRetry(urls[1]),"Actual request failure exposes retry");
            media.Get(urls[1]);Check(host.Jobs.Count==4,"No automatic retry loop");
            media.Retry(urls[1]);Check(host.Jobs.Count==5,"Actual explicit retry starts request");
            host.Complete(2,true,new byte[1]);Check(media.CanRetry(urls[2]),"Decode failure is retryable");
            host.Complete(3,true,Pixels(64,64));host.Complete(4,true,Pixels(64,64));
            Check(media.Get(urls[1])!=null&&!media.CanRetry(urls[1]),"Retry success clears failure");
            int jobs=host.Jobs.Count;
            for(int i=0;i<3;i++){string url="https://cdn.wikimg.net/large"+i+".png";media.Get(url);host.Complete(jobs+i,true,Pixels(4096,4096));}
            Check(media.CachedBytes<=ImageBudget.DefaultBytes&&media.CachedCount<=24,"Actual texture cache respects both limits");
            Check(media.Get(urls[0])!=null,"Evicted texture restores from disk cache");
            media.Dispose();Check(media.CachedBytes==0&&media.CachedCount==0,"Dispose clears live textures");
            Check(media.Get(urls[0])==null,"Disposed loader cannot restart");
            string blocked=Path.Combine(cache,"not-a-directory");File.WriteAllText(blocked,"file");
            var secondHost=new UnityEngine.MonoBehaviour();var second=Make(secondHost,blocked);
            second.Get(urls[0]);secondHost.Complete(0,true,Pixels(64,64));
            Check(second.Get(urls[0])!=null&&!second.CanRetry(urls[0]),"Disk cache failure does not hide decoded texture");
            second.Get(urls[1]);var active=(UnityEngine.Networking.UnityWebRequest)secondHost.Jobs[1].Current;
            second.Dispose();Check(active.Aborted,"Dispose aborts active network requests");
            secondHost.Complete(1,false,new byte[0]);Check(active.Disposed,"Aborted request is disposed by coroutine finally");
        }
        finally { I18n.English=false;Directory.Delete(cache,true); }
        return count+" actual media-loader assertions passed (engine/network doubles)";
    }
}

using System;
namespace UnityEngine {
public static class PlayerPrefs {
private static System.Collections.Generic.Dictionary<string,string> Values=new System.Collections.Generic.Dictionary<string,string>();
public static string GetString(string key,string fallback)=>Values.TryGetValue(key,out var value)?value:fallback;
public static void SetString(string key,string value){Values[key]=value;}public static void Save(){}
}}
namespace UnityEngine.Networking {
public class UploadHandlerRaw {public UploadHandlerRaw(byte[] data){}}
public class DownloadHandlerBuffer {public string text="";}
public class UnityWebRequest:System.IDisposable {
public enum Result {Success,ConnectionError,ProtocolError,DataProcessingError}
public long responseCode=200;public Result result=Result.Success;public string error="";
public UploadHandlerRaw uploadHandler; public DownloadHandlerBuffer downloadHandler;
public UnityWebRequest(string url,string method){}public void SetRequestHeader(string key,string value){}
public object SendWebRequest()=>null;public void Dispose(){}
}}

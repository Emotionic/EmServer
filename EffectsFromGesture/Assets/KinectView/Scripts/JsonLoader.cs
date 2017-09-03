using System.IO;
using UnityEngine;

public static class JsonLoader<T>
{
    /// <summary>
    /// jsonファイルを読み込みます。
    /// </summary>
    /// <param name="filepath">ファイル名</param>
    /// <param name="absolutePath">絶対パスかどうか</param>
    public static T Load(string filepath, bool absolutePath = false)
    {
        T obj = default(T);
        var _path = absolutePath ? filepath : System.Environment.CurrentDirectory + "\\" + filepath;

        if (File.Exists(filepath))
        {
            // 存在しているのでロード
            using (var _sr = new StreamReader(_path, System.Text.Encoding.UTF8))
            {
                var json = _sr.ReadToEnd();
                obj = JsonUtility.FromJson<T>(json);
            }
        }

        return obj;
    }

    /// <summary>
    /// jsonファイルに書き込みます。もしファイルが存在しない場合は、新しく作成します。
    /// </summary>
    /// <param name="obj">オブジェクト</param>
    /// <param name="filepath">ファイル名</param>
    public static void Save(T obj, string filepath, bool absolutePath = false)
    {
        var _path = absolutePath ? filepath : System.Environment.CurrentDirectory + "\\" + filepath;

        using (var _sw = new StreamWriter(_path, false, System.Text.Encoding.UTF8))
        {
            var json = JsonUtility.ToJson(obj, true);
            _sw.Write(json);
        }
    }

}

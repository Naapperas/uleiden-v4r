using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Text;

using Proyecto26;

public class LOGGER : Singleton<LOGGER>
{
    protected LOGGER() { } // singleton only - no constructor

    public int userId { get; private set; }
    public string userName { get; private set; }

    Query query;
    CryptLib crypt;
    bool waitingForServer;

    string iv;
    // static string plainkey = "XAQSJ24dpj46J28w";
    static string plainkey = "le99AGxBQMfsO3gA";
    static string hashedkey = CryptLib.getHashSha256(plainkey, 16);
    // static string postkey = "Xh42g8bXv5HLsDUD";
    static string postkey = "6QUITHAyoYm8RJsR";

    float updateFreq = 5F;
    List<JSONTimeseries> postStack;

    internal bool loggerReady;

    void Awake()
    {
        query = new Query();
        crypt = new CryptLib();
    }

    public void Init()
    {
        postStack = new List<JSONTimeseries>();

        if (GM.Instance.game.logging == HLP.LogMode.LOG)
        {
            InvokeRepeating("PostTimeseriesStack", 0F, updateFreq);
        }

        if (GM.Instance.game.logging == HLP.LogMode.NOLOG)
        {
            Debug.LogWarning("Logging Disabled!");
        }

    }

    #region Encryption / Decryption

    string Encrypt(string message)
    {
        iv = CryptLib.GenerateRandomIV(16);
        return crypt.encrypt(message, hashedkey, iv) + iv;
    }

    public void PrintEncryptedSettings()
    {

        try
        {
            string settingsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "settings.json");
            string settingsJSON = System.IO.File.ReadAllText(settingsPath);

            string _iv = CryptLib.GenerateRandomIV(16);
            string _msg = crypt.encrypt(settingsJSON, hashedkey, _iv);

            string _encodediv = Convert.ToBase64String(Encoding.UTF8.GetBytes(_iv));
            string _encodedmsg = Convert.ToBase64String(Encoding.UTF8.GetBytes(_msg));

            Debug.Log(_encodediv);
            Debug.Log(_encodedmsg);
        }
        catch
        {
            Debug.Log("Could not load settings");
        }

    }

    string Decrypt(string message, string foreignIv)
    {
        return crypt.decrypt(message, hashedkey, foreignIv);
    }

    public string DecryptSettings(string message, string foreignIv)
    {
        string _decodediv = Encoding.UTF8.GetString(Convert.FromBase64String(foreignIv));
        string _decodedmsg = Encoding.UTF8.GetString(Convert.FromBase64String(message));

        return crypt.decrypt(_decodedmsg, hashedkey, _decodediv);
    }

    public string GetPostKey()
    {
        return Encrypt(postkey);
    }

    #endregion

    #region Query Callbacks

    public void CB_SetServerId(JSONPostNewReply response)
    {
        if (userId > 0)
        {
            Debug.LogWarning("User already posted!");
            return;
        }

        userId = response.id;
        userName = response.user;

        GM.Instance.game.patternsActive = response.patterns;
        GM.Instance.game.contextActive = response.context;

        if (System.Enum.TryParse(response.style, out HLP.Style gamestyleEnum))
        {
            GM.Instance.game.gameStyle = gamestyleEnum;
        }
        else
        {
            Debug.LogWarning("Could not parse gameStyle - using internal");
        }

        if (System.Enum.TryParse(response.direction, out HLP.PlayDirection playdirEnum))
        {
            GM.Instance.game.playDirection = playdirEnum;
        }
        else
        {
            Debug.LogWarning("Could not parse playDirection - using internal");
        }

        Debug.Log(string.Format("Server assigned ID: {0}, USER: {1}, STYLE: {2}, PATTERNS: {3}, DIR: {4}, TXT: {5}",
            response.id, response.user, response.style, response.patterns.ToString(), response.direction, response.context.ToString()));
        waitingForServer = false;
        loggerReady = true;
    }

    #endregion

    void PostTimeseriesStack()
    {
        if (!loggerReady) return;

        if (postStack.Count <= 0)
        {
            Debug.Log("No updates to post");
            return;
        }

        Query.PostTimeseriesUpdate(postStack, this.userId, this.userName);

        // TODO: Before moving the stack, move it to a second stack that waits for server confirmation
        postStack.Clear();
    }

    string returnCurrentMilliEpoch()
    {
        int cur_time = (int)(System.DateTime.UtcNow - HLP.epochStart).TotalSeconds;
        return cur_time.ToString() + "." + System.DateTime.UtcNow.Millisecond.ToString("000");
    }

    public void InitSession()
    {
        if (GM.Instance.game.logging == HLP.LogMode.NOLOG) return;


        if (userId > 0)
        {
            Debug.LogWarning("User already posted!");
            return;
        }
        else if (waitingForServer)
        {
            Debug.LogWarning("Active Request Sent Already");
            return;
        }

        waitingForServer = true;

        string platform = Application.platform.ToString();
        platform = platform.ToUpper();

        Query.PostNewUser(platform, returnCurrentMilliEpoch());
    }

    public void EndSession()
    {
        Query.PostSessionEnd(this.userId, this.userName, returnCurrentMilliEpoch());
    }

    public void AddToTimeseries(string type, string content)
    {
        if (GM.Instance.game.logging == HLP.LogMode.NOLOG || !loggerReady) return;

        JSONTimeseries ts = new JSONTimeseries();
        ts.userdata_id = userId;
        ts.timestamp = returnCurrentMilliEpoch();
        ts.logtype = type;
        ts.logline = content;

        postStack.Add(ts);
    }



}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class JSONPostNewUser
{
    public string key;
    public string parameters;
    public string timestamp;

    public override string ToString()
    {
        return UnityEngine.JsonUtility.ToJson(this, true);
    }
}


[System.Serializable]
public class JSONPostNewReply
{
    public int id;
    public string user;
    public string style;
    public bool patterns;
    public string direction;
    public bool context;
}

[System.Serializable]
public class JSONPostSessionEnd
{
    public string key;
    public int id;
    public string user;
    public string timestamp;

    public override string ToString()
    {
        return UnityEngine.JsonUtility.ToJson(this, true);
    }
}

[System.Serializable]
public class JSONTimeseries
{
    public int userdata_id;
    public string timestamp;
    public string logtype;
    public string logline;

    public override string ToString()
    {
        return UnityEngine.JsonUtility.ToJson(this, true);
    }
}

[System.Serializable]
public class JSONTimeseriesList
{
    public string key;
    public int id;
    public string user;
    public List<JSONTimeseries> postdata;

    public override string ToString()
    {
        return UnityEngine.JsonUtility.ToJson(this, true);
    }
}




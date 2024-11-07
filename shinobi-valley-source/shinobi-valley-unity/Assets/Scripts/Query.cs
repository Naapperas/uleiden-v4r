using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Proyecto26;

/// <summary>
/// Class to handle all queries to the server
/// </summary>
public class Query
{

    static string basePath = "http://research.dandyus.com/shinobivalleyserver";


    public static void PostNewUser(string _platform, string _timestamp)
    {
        RestClient.Post<JSONPostNewReply>(GetFullUrl("session/postnew"), new JSONPostNewUser
        {
            key = LOGGER.Instance.GetPostKey(),
            parameters = string.Format("{0}", _platform),
            timestamp = _timestamp
        }).Then(response =>
        {
            LOGGER.Instance.CB_SetServerId(response);
        });
    }

    public static void PostSessionEnd(int _id, string _user, string _timestamp)
    {
        RestClient.Post(GetFullUrl("session/postend"), new JSONPostSessionEnd
        {
            key = LOGGER.Instance.GetPostKey(),
            id = _id,
            user = _user,
            timestamp = _timestamp
        }).Then(response =>
        {
            Debug.Log(response.Text );
        });
    }

    public static void PostTimeseriesUpdate(List<JSONTimeseries> input, int _id, string _user)
    {
        RestClient.Post(GetFullUrl("timeseries/post"), new JSONTimeseriesList
        {
            key = LOGGER.Instance.GetPostKey(),
            id = LOGGER.Instance.userId,
            user = LOGGER.Instance.userName,
            postdata = input
        }).Then(response =>
        {
            Debug.Log(response.Text );
        });


    }


    private static string GetFullUrl(string url)
    {
        return basePath + "/" + url;
    }
}

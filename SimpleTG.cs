using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

// 20220416 v0.1
// 20220417 v0.11
// a simple TigerGraph class with no dependencies that one can just drop into any Unity project
// private variables such as url, tokenSecret and graphName are exposed as public variables in the inspector (fill them in!)

// Usage is super simple:
//  1) Subscribe to delegate WWWDataParseMethod TokenReturned when token is returned
//  2) BeginGetToken() will populate CONST_Token (called in Start() by default)
//  3) Query(queryname,querystring,callback) to call a particular queryname and have response returned in callback
//  4) GetVertices/GetEdges

namespace TigerGraphUnity {
    public class Request_TokenRaw
    {
        public string secret;public string graph;public int lifetime = 86400;
        public Request_TokenRaw(string s,string g)
        {
            secret = s; graph = g;
        }
    }
    public class Response_RequestToken
    {
        public string code;
        public int expiration;
        public string error;
        public string message;
        public string token;
    }
    public class Response_Query
    {
        //TODO add the other params that are not mission critical
        public Response_Results[] results;
        
    }
    [System.Serializable]
    public class Response_Results
    {
        public Response_QuerySelectVertex[] tprint;
    }
    [System.Serializable]
    public class Response_QuerySelectVertex
    {
        public string v_id;
        public string v_type;
    } 

    public class SimpleTG : MonoBehaviour
    {
        public string CONST_TokenSecret;
        string CONST_Token;
        public string CONST_Graph_Name;

        const int CONST_PORT = 9000;
        const string CONST_PROTOCOL = "https";
        public string CONST_URL;

        public WWWDataParseMethod TokenReturned;

        private void Awake()
        {
            if (string.IsNullOrEmpty(CONST_TokenSecret)) Debug.LogError(">>> !!! SHOWSTOPPING ERROR: TokenSecret must be set!");
        }

        private void Start()
        {
            BeginGetToken();
        }

        public void BeginGetToken()
        {
            StartCoroutine(RequestToken(CONST_TokenSecret, CONST_Graph_Name));
        }

        IEnumerator RequestToken(string tokensecret,string graphname)
        {
            string urlRequestToken = url_Base() + "requesttoken";

            Request_TokenRaw rtr = new Request_TokenRaw(tokensecret,graphname);
            string raw = JsonUtility.ToJson(rtr);  
            UnityWebRequest www = UnityWebRequest.Post(urlRequestToken, UnityWebRequest.kHttpVerbPOST);
            www.SetRequestHeader("Content-Type", "application/json");
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(raw));
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("POST ERROR: " + www.url + " " + www.error); 
            }
            else
            {
                // store token!
                print(www.downloadHandler.text);
                Response_RequestToken rt = JsonUtility.FromJson<Response_RequestToken>(www.downloadHandler.text);
                CONST_Token = rt.token;

                print("CONST_Token " + rt.token);
                TokenReturned?.Invoke(rt.token);

                // test
                //Query("AddTask", "uuid_task=uuid&name_task=testing%20123&order_task=0&dt=2022-01-01 00:00:10&uuid_user=testuser", null);
            }
        }

        public delegate void WWWDataParseMethod(string s);
        public void Query(string queryname,string querystring,WWWDataParseMethod callme,string graphname=null)
        {
            if (graphname == null) graphname = CONST_Graph_Name;
            string url = url_Base() + "query" + "/" + graphname + "/" + queryname + "?" + querystring;
            StartCoroutine(BearerGet(url, callme,graphname));
        }
         
        public void GetEdges(string vertextype,string sourcevertexid, WWWDataParseMethod callme,string graphname = null)
        {
            if (graphname == null) graphname = CONST_Graph_Name;
            string url = url_Base() + "graph/" + graphname + "/edges?source_vertex_type=" + vertextype +"&source_vertex_id="+sourcevertexid;
            StartCoroutine(BearerGet(url, callme, graphname));
        }

        // helper functions
        IEnumerator BearerGet(string url, WWWDataParseMethod callme, string graphname = null)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            // print(urlQuery);
            www.SetRequestHeader("Authorization", "Bearer " + CONST_Token);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("GET ERROR: " + www.url + " " + www.error);
            }
            else
            {
                print(www.downloadHandler.text);
                callme?.Invoke(www.downloadHandler.text);
            }
        }

        static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
          string url_Base()
        {
            return CONST_PROTOCOL + "://" + CONST_URL + ":" + CONST_PORT + "/";
        } 

    }

}
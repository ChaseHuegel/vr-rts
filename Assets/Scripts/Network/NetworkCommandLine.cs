using System.Collections.Generic;
using MLAPI;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management; 
using Valve.VR;
public class NetworkCommandLine : MonoBehaviour
{
   private NetworkManager netManager;

   void Start()
   {
       netManager = GetComponentInParent<NetworkManager>();

       if (Application.isEditor) return;

       var args = GetCommandlineArgs();

       if (args.TryGetValue("-mlapi", out string mlapiValue))
       {
           switch (mlapiValue)
           {
                case "server":
                    netManager.StartServer();
                    XRGeneralSettings.Instance.Manager.DeinitializeLoader();                    
                    //PlayerManager.instance.gameObject.transform.GetChild(0).gameObject.SetActive(false);
                    //PlayerManager.instance.gameObject.transform.GetChild(1).gameObject.SetActive(true);
                    break;

                case "host":
                    netManager.StartHost();
                    break;

                case "client":
            
                    netManager.StartClient();
                    break;
           }
       }
   }

   private Dictionary<string, string> GetCommandlineArgs()
   {
       Dictionary<string, string> argDictionary = new Dictionary<string, string>();

       var args = System.Environment.GetCommandLineArgs();

       for (int i = 0; i < args.Length; ++i)
       {
           var arg = args[i].ToLower();
           if (arg.StartsWith("-"))
           {
               var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
               value = (value?.StartsWith("-") ?? false) ? null : value;

               argDictionary.Add(arg, value);
           }
       }
       return argDictionary;
   }
}
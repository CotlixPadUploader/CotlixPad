


using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using GorillaNetworking;

namespace ColtixPad.Classes
{
    public class TrackerClient : MonoBehaviour
    {
        // Change this to your server's IP/domain
        private const string SERVER_URL = "http://localhost:3000";

        private const float HEARTBEAT_INTERVAL = 30f;

        private string userId;
        private Coroutine heartbeatCoroutine;

        // Owner ID stored XOR-encoded so it never appears as a plain string
        // in the compiled binary. Key: 0x5F. Decompiling will only show bytes.
        private static readonly byte[] _ownerId = { 108, 108, 102, 25, 111, 109, 105, 108, 110, 102, 109, 28, 102, 26, 108, 108 };
        private const byte _xorKey = 0x5F;

        private static string DecodeOwnerId()
        {
            char[] chars = new char[_ownerId.Length];
            for (int i = 0; i < _ownerId.Length; i++)
                chars[i] = (char)(_ownerId[i] ^ _xorKey);
            return new string(chars);
        }

        /// <summary>
        /// Returns true only if the given rig's PlayFab user ID matches the owner.
        /// The ID is decoded from an obfuscated byte array at runtime so it never
        /// exists as a readable string in the compiled DLL.
        /// </summary>
        public static bool IsOwner(VRRig rig)
        {
            if (rig == null) return false;
            try { return string.Equals(rig.Creator?.UserId, DecodeOwnerId(), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        void Start()
        {
            userId = SystemInfo.deviceUniqueIdentifier;
            StartHeartbeat();
        }

        void OnDestroy()
        {
            StopHeartbeat();
            StartCoroutine(SendLeave());
        }

        public void StartHeartbeat()
        {
            if (heartbeatCoroutine != null) StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
        }

        public void StopHeartbeat()
        {
            if (heartbeatCoroutine != null)
            {
                StopCoroutine(heartbeatCoroutine);
                heartbeatCoroutine = null;
            }
        }

        private IEnumerator HeartbeatLoop()
        {
            while (true)
            {
                yield return SendHeartbeat();
                yield return new WaitForSeconds(HEARTBEAT_INTERVAL);
            }
        }

        private IEnumerator SendHeartbeat()
        {
            string username = GetUsername();
            string roomCode = GetRoomCode();

            string json = $"{{\"userId\":\"{userId}\",\"username\":\"{EscapeJson(username)}\",\"roomCode\":\"{EscapeJson(roomCode)}\"}}";

            using (UnityWebRequest req = new UnityWebRequest(SERVER_URL + "/api/heartbeat", "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();
            }
        }

        private IEnumerator SendLeave()
        {
            string json = $"{{\"userId\":\"{userId}\"}}";

            using (UnityWebRequest req = new UnityWebRequest(SERVER_URL + "/api/leave", "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();
            }
        }

        private string GetUsername()
        {
            try { return PlayerPrefs.GetString("playerName", "Unknown"); }
            catch { return "Unknown"; }
        }

        private string GetRoomCode()
        {
            try
            {
                if (Photon.Pun.PhotonNetwork.InRoom)
                    return Photon.Pun.PhotonNetwork.CurrentRoom.Name;
                return "Menu";
            }
            catch { return "Menu"; }
        }

        private string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

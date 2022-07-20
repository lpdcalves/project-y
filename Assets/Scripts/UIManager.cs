using Core.Singletons;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectY
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] public Transform connectUI;
        [SerializeField] public Transform gameStatusUI;
        [SerializeField] public Transform leaveUI;

        [SerializeField] private Button startServerButton;
        [SerializeField] private Button startHostButton;
        [SerializeField] private Button startClientButton;
        private Button leaveButton;

        [SerializeField] private TextMeshProUGUI playersInGameText;
        [SerializeField] public TMP_InputField joinCodeInput;
        [SerializeField] private TextMeshProUGUI joinCodeText;

        public string joinCodeFromRelayServer = "";


        void Update()
        {
            playersInGameText.text = $"Players in game: {MatchNetworkManager.Instance.playerCount.Value}";
        }

        void Start()
        {
            connectUI.gameObject.SetActive(true);
            gameStatusUI.gameObject.SetActive(false);
            leaveUI.gameObject.SetActive(false);

            leaveButton = leaveUI.GetComponentInChildren<Button>();

            // START SERVER
            startServerButton?.onClick.AddListener(() =>
            {
                MatchNetworkManager.Instance.StartServer();
            });

            // START HOST
            startHostButton?.onClick.AddListener(async () =>
            {
                connectUI.gameObject.SetActive(false);
                gameStatusUI.gameObject.SetActive(true);
                leaveUI.gameObject.SetActive(true);

                if (!RelayManager.Instance.IsRelayEnabled)
                {
                    SetJoinCodeText(joinCodeInput.text);
                }
                await MatchNetworkManager.Instance.StartHost();
            });

            // START CLIENT
            startClientButton?.onClick.AddListener(async () =>
            {
                connectUI.gameObject.SetActive(false);
                gameStatusUI.gameObject.SetActive(true);
                leaveUI.gameObject.SetActive(true);

                if (!RelayManager.Instance.IsRelayEnabled)
                {
                    SetJoinCodeText(joinCodeInput.text);
                }
                await MatchNetworkManager.Instance.StartClient(joinCodeInput.text);
            });

            // LEAVE MATCH
            leaveButton?.onClick.AddListener(() =>
            {
                connectUI.gameObject.SetActive(true);
                gameStatusUI.gameObject.SetActive(false);
                leaveUI.gameObject.SetActive(false);

                MatchNetworkManager.Instance.LeaveMatch();
            });
        }

        public void SetJoinCodeText(string joinCode)
        {
            joinCodeFromRelayServer = joinCode;
            joinCodeText.text = joinCode;
        }
    }
}
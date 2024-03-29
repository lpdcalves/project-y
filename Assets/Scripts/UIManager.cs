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
        [SerializeField] public Transform gunSelectionUI;
        [SerializeField] public Transform gunStatusUI;
        [SerializeField] public Transform PistolaUI;
        [SerializeField] public Transform RifleUI;
        [SerializeField] public Transform ChatUI;
        [SerializeField] public Transform GameVignetteUI;
        [SerializeField] public Transform ReloadUI;

        [SerializeField] private Button startServerButton;
        [SerializeField] private Button startHostButton;
        [SerializeField] private Button startClientButton;
        private Button leaveButton;

        [SerializeField] private TextMeshProUGUI playersInGameText;
        [SerializeField] public TMP_InputField joinCodeInput;
        [SerializeField] private TextMeshProUGUI joinCodeText;
        [SerializeField] public TMP_InputField playerNameInput;

        public TextMeshProUGUI currAmmo;
        public TextMeshProUGUI maxAmmo;

        public string joinCodeFromRelayServer = "";
        public string playerName;


        void Update()
        {
            playersInGameText.text = $"Players in game: {MatchNetworkManager.Instance.playerCount.Value}";
        }

        void Start()
        {
            connectUI.gameObject.SetActive(true);
            gameStatusUI.gameObject.SetActive(false);
            leaveUI.gameObject.SetActive(false);
            gunSelectionUI.gameObject.SetActive(false);
            gunStatusUI.gameObject.SetActive(false);
            ChatUI.gameObject.SetActive(false);
            GameVignetteUI.gameObject.SetActive(false);
            ReloadUI.gameObject.SetActive(false);

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
                gunSelectionUI.gameObject.SetActive(true);
                gunStatusUI.gameObject.SetActive(true);
                ChatUI.gameObject.SetActive(true);

                playerName = playerNameInput.text;

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
                gunSelectionUI.gameObject.SetActive(true);
                gunStatusUI.gameObject.SetActive(true);
                ChatUI.gameObject.SetActive(true);

                playerName = playerNameInput.text;

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
                gunSelectionUI.gameObject.SetActive(false);
                gunStatusUI.gameObject.SetActive(false);
                ChatUI.gameObject.SetActive(false);

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
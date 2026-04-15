using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class MinigameTileUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Data")]
    [SerializeField] private MinigameDefinition definition;

    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text playersText;
    [SerializeField] private GameObject teamsIcon;
    [SerializeField] private GameObject ffaIcon;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoRender;

    [Header("Hover Scale")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float scaleSpeed = 8f;

    private Vector3 defaultScale;
    private Vector3 targetScale;

    private void Awake()
    {
        defaultScale = transform.localScale;
        targetScale = defaultScale;

        ApplyDefinition();
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }

    public void ApplyDefinition()
    {
        if (definition == null) return;

        nameText.text = definition.sceneName;
        playersText.text = $"{definition.amountOfPlayers}";
        teamsIcon.SetActive(definition.isTeamsAvailable);
        ffaIcon.SetActive(definition.isFFAAvailable);

        if (videoPlayer != null)
        {
            videoPlayer.clip = definition.previewVideo;
            videoPlayer.targetTexture = definition.renderTexture;
        }

        if (videoRender != null)
        {
            videoRender.texture = definition.renderTexture;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = defaultScale * hoverScale;

        if (videoPlayer != null)
            videoPlayer.Play();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = defaultScale;

        if (videoPlayer != null)
            videoPlayer.Stop();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (definition == null) return;

        // Host-only
        if (!NetworkClient.activeHost)
            return;

        if (NetworkPlaylistManager.Instance == null)
        {
            Debug.LogError("NetworkPlaylistManager.Instance is null. Put it on a NetworkIdentity in Lobby scene.");
            return;
        }

        NetworkPlaylistManager.Instance.Host_TryAddMinigame(definition.sceneName);
    }
}
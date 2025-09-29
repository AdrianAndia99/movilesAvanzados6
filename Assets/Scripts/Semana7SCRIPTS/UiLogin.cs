using System;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.InputSystem.Composites;
using UnityEngine.UI;

public class UiLogin : MonoBehaviour
{
    [SerializeField] private Transform loginPanel;
    [SerializeField] private Transform userPanel;


    [SerializeField] private Button loginButton;
    [SerializeField] private TMP_Text IDText;
    [SerializeField] private TMP_Text NameText;


    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button updateNameButton;

    [SerializeField] private UnityAuth unityAuth;

    void Start()
    {
        loginPanel.gameObject.SetActive(true);
        userPanel.gameObject.SetActive(false);
    }
    void OnEnable()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        unityAuth.OnSignedIn += HandleSignedIn;
        updateNameButton.onClick.AddListener(OnUpdateNameButtonClicked);
        unityAuth.OnNameUpdated += HandleNameUpdated;
    }

    private void HandleNameUpdated(string obj)
    {
        NameText.text = "Name: " + obj;
    }

    private async void OnUpdateNameButtonClicked()
    {
        await unityAuth.UpdateName(nameInputField.text);
    }

    void OnDisable()
    {
        loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        unityAuth.OnSignedIn -= HandleSignedIn;
        updateNameButton.onClick.RemoveListener(OnUpdateNameButtonClicked);
    }
    private void HandleSignedIn(PlayerInfo playerInfo, string playerName)
    {
        loginPanel.gameObject.SetActive(false);
        userPanel.gameObject.SetActive(true);

        IDText.text = "ID: " + playerInfo.Id;
        NameText.text = "Name: " + playerName;
    }

    private async void OnLoginButtonClicked()
    {
        await unityAuth.InitSignIn();
    }

}

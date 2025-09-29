using UnityEngine;

using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;
using System;
using Unity.Services.Authentication.PlayerAccounts;
using System.Collections.Generic;
public class UnityAuth : MonoBehaviour
{
    public event Action<PlayerInfo, string> OnSignedIn;
    public event Action<string> OnNameUpdated;
    private PlayerInfo playerInfo;
    private async void Start()
    {
        await UnityServices.InitializeAsync();
        Debug.Log(UnityServices.State);
        SetUpEvent();
        PlayerAccountService.Instance.SignedIn += SignIn;
    }

    public async Task InitSignIn()
    {
        await PlayerAccountService.Instance.StartSignInAsync();
    }

    private void SetUpEvent()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("player id: " + AuthenticationService.Instance.PlayerId);
            Debug.Log("acces token: " + AuthenticationService.Instance.AccessToken);
        };
        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.Log("User sign in failed: " + err);
        };
        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("User signed out");
        };
        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("User session expired: ");
        };
    }

    private async void SignIn()
    {
        try
        {
            await SignInWithUnity();
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }
    private async Task SignInWithUnity()
    {
        try
        {
            string accessToken = PlayerAccountService.Instance.AccessToken;
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
            playerInfo = AuthenticationService.Instance.PlayerInfo;
            var name = await AuthenticationService.Instance.GetPlayerNameAsync();
            OnSignedIn?.Invoke(playerInfo, name);
            Debug.Log("User signed in successfully ");
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }
    }

    public async Task UpdateName(string newName)
    {

        await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
        Debug.Log("Player name updated successfully");
        var name = await AuthenticationService.Instance.GetPlayerNameAsync();

        OnNameUpdated?.Invoke(name);
    }

    public async Task DeleteUnityAsync()
    {
        try
        {
            await AuthenticationService.Instance.DeleteAccountAsync();
            Debug.Log("Player data deleted successfully");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    //cloud
    public async void saveData(string key, string value)
    {
        var playerdata = new Dictionary<string, object>()
        {
            {key, value}
        };
        await CloudSaveService.Instance.Data.Player.SaveAsync(playerdata);
    }

    private async void LoadData(string key)
    {
        var playerdata = await CloudSaveService.Instance.Data.Player.LoadAsync(
            new HashSet<string> { key }
        );
    }
}
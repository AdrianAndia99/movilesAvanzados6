using UnityEngine;

using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;

public class AnonimAuth : MonoBehaviour
{
    private async void Start()
    {
        await UnityServices.InitializeAsync();
        Debug.Log(UnityServices.State);
        SetUpEvent();
        await SignInAnonymously();
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


    private async Task SignInAnonymously()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            //Debug.Log("Signed in anonymously");
           // Debug.Log("player id: " + AuthenticationService.Instance.PlayerId);
        }
        catch (AuthenticationException ex)
        {
            Debug.Log(ex);
        }
        catch (RequestFailedException ex)
        {
            Debug.Log(ex);
        }
    }
}

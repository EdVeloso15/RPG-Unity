using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UGSInitializer : MonoBehaviour
{
    public static UGSInitializer Instance { get; private set; }

    public bool IsReady { get; private set; }

    public string PlayerId =>
        AuthenticationService.Instance.IsSignedIn
            ? AuthenticationService.Instance.PlayerId
            : string.Empty;

    public string CurrentPlayerName =>
        AuthenticationService.Instance.IsSignedIn
            ? AuthenticationService.Instance.PlayerName ?? string.Empty
            : string.Empty;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        if (IsReady)
            return;

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();

                Debug.Log(
                    "[UGS] Unity Gaming Services inicializado."
                );
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }

            IsReady = true;

            Debug.Log(
                $"[UGS] Jogador autenticado. " +
                $"Player ID: {AuthenticationService.Instance.PlayerId}"
            );
        }
        catch (AuthenticationException ex)
        {
            IsReady = false;

            Debug.LogError(
                $"[UGS] Falha na autenticação: {ex}"
            );
        }
        catch (RequestFailedException ex)
        {
            IsReady = false;

            Debug.LogError(
                $"[UGS] Falha ao inicializar os serviços: {ex}"
            );
        }
        catch (Exception ex)
        {
            IsReady = false;

            Debug.LogError(
                $"[UGS] Erro inesperado: {ex}"
            );
        }
    }

    /// <summary>
    /// Define o nome que será utilizado pelo jogador.
    /// A Authentication não aceita espaços no Player Name.
    /// Por isso normalizamos os espaços para '_'.
    /// </summary>
    public async Task<bool> SetPlayerNameAsync(
        string requestedName)
    {
        if (!IsReady)
        {
            await InitializeAsync();
        }

        if (!IsReady)
        {
            Debug.LogError(
                "[UGS] Não é possível definir o nome. " +
                "Os serviços não estão prontos."
            );

            return false;
        }

        string playerName =
            NormalizePlayerName(requestedName);

        try
        {
            await AuthenticationService.Instance
                .UpdatePlayerNameAsync(playerName);

            Debug.Log(
                $"[UGS] Nome do jogador atualizado: " +
                $"{AuthenticationService.Instance.PlayerName}"
            );

            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError(
                $"[UGS] Falha de autenticação ao definir nome: {ex}"
            );

            return false;
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError(
                $"[UGS] Falha ao definir nome do jogador: {ex}"
            );

            return false;
        }
    }

    private string NormalizePlayerName(
        string requestedName)
    {
        if (string.IsNullOrWhiteSpace(requestedName))
        {
            requestedName = "Player";
        }

        requestedName =
            requestedName.Trim();

        char[] characters =
            requestedName.ToCharArray();

        for (int i = 0; i < characters.Length; i++)
        {
            if (char.IsWhiteSpace(characters[i]))
            {
                characters[i] = '_';
            }
        }

        string normalized =
            new string(characters);

        if (normalized.Length > 50)
        {
            normalized =
                normalized.Substring(0, 50);
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "Player";
        }

        return normalized;
    }

    private async void OnApplicationQuit()
    {
        // Os serviços serão encerrados pelo Unity.
        // Não precisamos executar operações de Session aqui.
        await Task.Yield();
    }
}
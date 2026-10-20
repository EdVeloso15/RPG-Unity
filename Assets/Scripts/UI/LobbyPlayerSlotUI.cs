using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Representa visualmente um jogador dentro do Lobby.
///
/// Diferentemente da versão uGUI, esta classe não é MonoBehaviour
/// e não fica anexada a um GameObject.
/// Ela controla diretamente um VisualElement do UI Toolkit.
/// </summary>
public sealed class LobbyPlayerSlotUI
{
    private readonly VisualElement root;

    private readonly VisualElement avatar;
    private readonly VisualElement playerInfo;

    private readonly Label playerName;
    private readonly Label playerRole;

    // =========================================================
    // NOVO:
    // Indicador visual de Ready.
    // =========================================================
    private readonly VisualElement readyIndicator;
    private readonly VisualElement readyActive;
    private readonly VisualElement readyInactive;


    public LobbyPlayerSlotUI(
        VisualElement rootElement)
    {
        root = rootElement;

        avatar =
            root.Q<VisualElement>("Avatar");

        playerInfo =
            root.Q<VisualElement>("PlayerInfo");

        playerName =
            root.Q<Label>("PlayerName");

        playerRole =
            root.Q<Label>("PlayerRole");

        // =====================================================
        // NOVO:
        // LocalPlayer foi removido.
        // Agora buscamos os elementos do ReadyIndicator.
        // =====================================================
        readyIndicator =
            root.Q<VisualElement>("ReadyIndicator");

        readyActive =
            root.Q<VisualElement>("ReadyActive");

        readyInactive =
            root.Q<VisualElement>("ReadyInactive");

        ValidateReferences();
    }


    /// <summary>
    /// Mostra os dados de um jogador.
    /// </summary>
    public void SetPlayer(
        LobbyPlayerData player)
    {
        root.style.display =
            DisplayStyle.Flex;

        if (avatar != null)
        {
            avatar.style.display =
                DisplayStyle.Flex;
        }

        if (playerInfo != null)
        {
            playerInfo.style.display =
                DisplayStyle.Flex;
        }

        if (playerName != null)
        {
            playerName.text =
                player.DisplayName;

            // =================================================
            // NOVO:
            // O jogador local é identificado pelo nome em
            // negrito.
            //
            // Os demais jogadores permanecem normais.
            // =================================================
            playerName.style.unityFontStyleAndWeight =
                player.IsLocalPlayer
                    ? FontStyle.Bold
                    : FontStyle.Normal;
        }

        if (playerRole != null)
        {
            playerRole.text =
                player.IsHost
                    ? "HOST"
                    : "JOGADOR";
        }

        // =====================================================
        // NOVO:
        // Atualiza o indicador visual de Ready.
        //
        // Essa informação vem diretamente de:
        // player.IsReady
        // =====================================================
        UpdateReadyIndicator(
            player.IsReady);
    }


    /// <summary>
    /// Mostra o slot como disponível.
    /// </summary>
    public void SetEmpty()
    {
        root.style.display =
            DisplayStyle.Flex;

        if (avatar != null)
        {
            avatar.style.display =
                DisplayStyle.None;
        }

        if (playerName != null)
        {
            playerName.text =
                "Aguardando jogador...";

            // =================================================
            // NOVO:
            // Slot vazio sempre usa texto normal.
            // =================================================
            playerName.style.unityFontStyleAndWeight =
                FontStyle.Normal;
        }

        if (playerRole != null)
        {
            playerRole.text =
                string.Empty;
        }

        // =====================================================
        // NOVO:
        // Não existe jogador neste slot.
        // Portanto o indicador de Ready fica oculto.
        // =====================================================
        HideReadyIndicator();
    }


    // =========================================================
    // NOVO:
    // Atualiza as duas imagens do indicador.
    // =========================================================
    private void UpdateReadyIndicator(
        bool isReady)
    {
        if (readyIndicator != null)
        {
            readyIndicator.style.display =
                DisplayStyle.Flex;
        }

        if (readyActive != null)
        {
            readyActive.style.display =
                isReady
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        if (readyInactive != null)
        {
            readyInactive.style.display =
                isReady
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
        }
    }


    // =========================================================
    // NOVO:
    // Esconde completamente o indicador quando o slot está
    // vazio.
    // =========================================================
    private void HideReadyIndicator()
    {
        if (readyIndicator != null)
        {
            readyIndicator.style.display =
                DisplayStyle.None;
        }
    }


    private void ValidateReferences()
    {
        if (playerName == null)
        {
            UnityEngine.Debug.LogError(
                "[LobbyPlayerSlotUI] " +
                "Label 'PlayerName' não encontrado."
            );
        }

        if (playerRole == null)
        {
            UnityEngine.Debug.LogError(
                "[LobbyPlayerSlotUI] " +
                "Label 'PlayerRole' não encontrado."
            );
        }

        // =====================================================
        // NOVO:
        // Validação dos elementos do Ready.
        // =====================================================

        if (readyIndicator == null)
        {
            UnityEngine.Debug.LogError(
                "[LobbyPlayerSlotUI] " +
                "VisualElement 'ReadyIndicator' não encontrado."
            );
        }

        if (readyActive == null)
        {
            UnityEngine.Debug.LogError(
                "[LobbyPlayerSlotUI] " +
                "VisualElement 'ReadyActive' não encontrado."
            );
        }

        if (readyInactive == null)
        {
            UnityEngine.Debug.LogError(
                "[LobbyPlayerSlotUI] " +
                "VisualElement 'ReadyInactive' não encontrado."
            );
        }
    }
}
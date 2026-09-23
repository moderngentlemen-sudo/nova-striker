using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Temporary four-player session HUD for local multiplayer validation.
    /// </summary>
    public sealed class StrikeTeamDebugHUD : MonoBehaviour
    {
        [SerializeField] private StrikeTeamSession session;

        private GUIStyle body;
        private GUIStyle header;

        private void Awake()
        {
            if (!session)
                session = StrikeTeamSession.Active;
        }

        private void OnGUI()
        {
            if (!session)
                session = StrikeTeamSession.Active;

            if (!session)
                return;

            EnsureStyles();

            float x = Screen.width - 350f;
            float y = 14f;
            float height = 170f;

            GUI.Box(
                new Rect(x, y, 336f, height),
                GUIContent.none
            );

            GUI.Label(
                new Rect(x + 14f, y + 10f, 300f, 24f),
                "STRIKE TEAM — LOCAL SESSION",
                header
            );

            string text =
                $"Players: {session.ActivePlayerCount}/4    " +
                $"Synergy: {session.Synergy:0}/{session.MaxSynergy:0}\n";

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                StrikerPlayerIdentity player =
                    session.GetPlayer(i);

                if (!player)
                {
                    text += $"P{i + 1}: —\n";
                    continue;
                }

                text +=
                    $"P{i + 1}: {player.Character} / " +
                    $"{player.TeamRole} / " +
                    $"{player.CommandRank}" +
                    $"{(player.IsCombatReady ? "" : " / DOWN")}\n";
            }

            GUI.Label(
                new Rect(x + 14f, y + 38f, 306f, 122f),
                text,
                body
            );
        }

        private void EnsureStyles()
        {
            if (body != null && header != null)
                return;

            header = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };

            body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12
            };
        }
    }
}
